using System.IO.Compression;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using UUSport.Api.Data;
using UUSport.Api.Features.Auth;
using UUSport.Api.Infrastructure;

namespace UUSport.Api.Features.Imports;

public static class ImportEndpoints
{
    private const long MaxFileBytes = 15 * 1024 * 1024;

    public static void MapImports(this IEndpointRouteBuilder app)
    {
        var g = app.MapGroup("/api/imports").RequireAuthorization(Policies.Staff);

        g.MapGet("/kinds", () => Results.Ok(ImportKinds.Fields.Select(k => new { kind = k.Key, fields = k.Value })));

        // Surum 2 hazirlik notu madde 1: zorunlu alanlari isaretli, sentetik ornekli bos sablon.
        // GET oldugu icin SecurityMiddleware'deki CSRF basligi gerekmez; dogrudan indirilebilir.
        g.MapGet("/template", (string kind) =>
        {
            var bytes = ImportService.BuildTemplate(kind);
            var fileName = $"uusport-{kind}-sablon.xlsx";
            return Results.File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
        });

        // CSRF korumasi ozel baslikla saglanir (SecurityMiddleware); form antiforgery'si kullanilmaz.
        g.MapPost("/inspect", async (IFormFile file, string kind, string? sheet, ImportService s) =>
        {
            var bytes = await Read(file);
            var (sheets, selected) = s.Inspect(bytes, kind, sheet);
            return Results.Ok(new { sheets, selected, studentSheet = ImportKinds.StudentSheet });
        }).DisableAntiforgery();

        g.MapPost("/preview", async (IFormFile file, string kind, string sheet, string mapping, ImportService s, CancellationToken ct) =>
        {
            var bytes = await Read(file);
            Dictionary<string, int> map;
            try { map = JsonSerializer.Deserialize<Dictionary<string, int>>(mapping) ?? []; }
            catch (JsonException) { throw new DomainException("mapping", "Sütun eşleştirmesi okunamadı."); }
            var batch = await s.PreviewAsync(bytes, file.FileName, kind, sheet, map, ct);
            return Results.Ok(new { batch.Id });
        }).DisableAntiforgery();

        g.MapGet("", async (AppDbContext db, CancellationToken ct) =>
            Results.Ok(await db.ImportBatches.OrderByDescending(b => b.CreatedAt).Take(100)
                .Select(b => new
                {
                    b.Id, b.Kind, b.FileName, b.SheetName, status = b.Status.ToString(), b.RowsRead, b.RowsValid, b.RowsError,
                    b.CreatedAt, b.AppliedAt, b.RevertedAt,
                }).ToListAsync(ct)));

        g.MapGet("/{id:guid}", async (Guid id, AppDbContext db, CancellationToken ct) =>
        {
            var b = await db.ImportBatches.SingleOrDefaultAsync(x => x.Id == id, ct) ?? throw new NotFoundException("Aktarım");
            var rows = await db.ImportRows.Where(r => r.BatchId == id).OrderBy(r => r.RowNumber)
                .Select(r => new { r.RowNumber, r.DataJson, r.IsValid, r.Errors, r.Warnings, r.PartyId }).ToListAsync(ct);
            return Results.Ok(new
            {
                batch = new { b.Id, b.Kind, b.FileName, b.SheetName, status = b.Status.ToString(), b.RowsRead, b.RowsValid, b.RowsError, b.CreatedAt, b.AppliedAt, b.RevertedAt },
                rows = rows.Select(r => new
                {
                    r.RowNumber, data = JsonSerializer.Deserialize<Dictionary<string, string?>>(r.DataJson),
                    r.IsValid, r.Errors, r.Warnings, r.PartyId,
                }),
                fields = ImportKinds.Fields[b.Kind],
            });
        });

        g.MapPost("/{id:guid}/apply", async (Guid id, ImportService s, CancellationToken ct) =>
        {
            await s.ApplyAsync(id, ct);
            return Results.Ok(new { ok = true });
        });

        g.MapPost("/{id:guid}/revert", async (Guid id, ImportService s, CancellationToken ct) =>
        {
            await s.RevertAsync(id, ct);
            return Results.Ok(new { ok = true });
        });
    }

    /// <summary>.xlsx aslinda bir zip'tir; sikistirma orani anormal yuksek bir dosya (zip bomb) 15 MB
    /// sinirini gecmeden bellekte devasa buyusun ac edebilir (S-19, X:SEC-06). Acmadan once merkezi
    /// dizinden ac sikistirilmamis toplam boyut kontrol edilir.</summary>
    private const long MaxUncompressedBytes = 200 * 1024 * 1024;

    private static async Task<byte[]> Read(IFormFile file)
    {
        if (file.Length == 0) throw new DomainException("file", "Dosya boş.");
        if (file.Length > MaxFileBytes) throw new DomainException("file", "Dosya 15 MB'tan büyük olamaz.");
        if (!file.FileName.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase))
            throw new DomainException("file", "Yalnız .xlsx biçimindeki Excel dosyaları desteklenir.");
        using var ms = new MemoryStream();
        await file.CopyToAsync(ms);
        var bytes = ms.ToArray();
        CheckNotZipBomb(bytes);
        return bytes;
    }

    private static void CheckNotZipBomb(byte[] bytes)
    {
        try
        {
            using var zip = new ZipArchive(new MemoryStream(bytes, writable: false), ZipArchiveMode.Read);
            long total = 0;
            foreach (var entry in zip.Entries)
            {
                total += entry.Length;
                if (total > MaxUncompressedBytes)
                    throw new DomainException("file", "Dosya içeriği açıldığında aşırı büyük olacak görünüyor; reddedildi.");
            }
        }
        catch (InvalidDataException)
        {
            throw new DomainException("file", "Dosya okunamadı. Yalnız .xlsx biçimindeki Excel dosyaları desteklenir.");
        }
    }
}
