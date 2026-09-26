using Microsoft.EntityFrameworkCore;
using UUSport.Api.Data;
using UUSport.Api.Domain;
using UUSport.Api.Features.Accounting;
using UUSport.Api.Features.Audit;
using UUSport.Api.Features.Auth;
using UUSport.Api.Infrastructure;

namespace UUSport.Api.Features.Parties;

public sealed record PartyInput(string Name, PartyKind Kind, string? Code, string? Phone, string? Email,
    string? TaxNumber, string? Notes, bool IsActive = true);

public sealed record AthleteInput(string FirstName, string LastName, string? GroupName, int? BirthYear, bool IsActive = true);

public static class PartyService
{
    public static async Task<string> NextCodeAsync(AppDbContext db, string prefix, CancellationToken ct)
    {
        var codes = await db.Parties.Where(p => p.Code.StartsWith(prefix)).Select(p => p.Code).ToListAsync(ct);
        var max = codes.Select(c => int.TryParse(c.AsSpan(prefix.Length), out var n) ? n : 0).DefaultIfEmpty(0).Max();
        return $"{prefix}{max + 1:000000}";
    }

    public static void Apply(Party p, PartyInput input)
    {
        p.Name = TextRules.CleanName(input.Name);
        if (p.Name.Length == 0) throw new DomainException("name", "Cari adı zorunlu.");
        p.Kind = input.Kind;
        p.Phone = TextRules.NormalizePhone(input.Phone);
        p.Email = TextRules.NormalizeEmail(input.Email);
        p.TaxNumber = string.IsNullOrWhiteSpace(input.TaxNumber) ? null : new string(input.TaxNumber.Where(char.IsAsciiDigit).ToArray());
        p.Notes = string.IsNullOrWhiteSpace(input.Notes) ? null : input.Notes.Trim();
        p.IsActive = input.IsActive;
        p.SearchKey = TextRules.SearchKey(p.Name, p.Code, p.Phone, p.Email, p.TaxNumber);
    }
}

public static class PartyEndpoints
{
    public static void MapParties(this IEndpointRouteBuilder app)
    {
        var g = app.MapGroup("/api/parties").RequireAuthorization(Policies.Staff);

        g.MapGet("", async (string? q, string? kind, bool? includeInactive, AppDbContext db, CancellationToken ct) =>
        {
            var query = db.Parties.AsQueryable();
            if (includeInactive != true) query = query.Where(p => p.IsActive);
            if (kind == "customer") query = query.Where(p => p.Kind == PartyKind.Customer || p.Kind == PartyKind.Both);
            if (kind == "supplier") query = query.Where(p => p.Kind == PartyKind.Supplier || p.Kind == PartyKind.Both);
            if (!string.IsNullOrWhiteSpace(q))
            {
                var key = TextRules.SearchKey(q);
                // Sporcu adiyla da veli carisi bulunur.
                var athleteParties = db.Athletes.Where(a => a.SearchKey.Contains(key)).Select(a => a.PartyId);
                query = query.Where(p => p.SearchKey.Contains(key) || athleteParties.Contains(p.Id));
            }
            var parties = await query.OrderBy(p => p.Name).Take(500).ToListAsync(ct);
            var ids = parties.Select(p => p.Id).ToList();
            var balances = await db.LedgerEntries.Where(e => ids.Contains(e.PartyId))
                .GroupBy(e => e.PartyId).Select(x => new { x.Key, Balance = x.Sum(e => e.Debit - e.Credit) })
                .ToDictionaryAsync(x => x.Key, x => x.Balance, ct);
            var athletes = await db.Athletes.Where(a => ids.Contains(a.PartyId) && a.IsActive)
                .Select(a => new { a.PartyId, Name = a.FirstName + " " + a.LastName }).ToListAsync(ct);
            var athletesByParty = athletes.GroupBy(a => a.PartyId).ToDictionary(x => x.Key, x => x.Select(a => a.Name).ToList());
            return Results.Ok(parties.Select(p => new
            {
                p.Id, p.Code, p.Name, kind = AppDbContext.ToSnake(p.Kind.ToString()), p.Phone, p.Email, p.IsActive,
                balance = balances.GetValueOrDefault(p.Id),
                athletes = athletesByParty.GetValueOrDefault(p.Id, []),
            }));
        });

        g.MapGet("/{id:guid}", async (Guid id, AppDbContext db, AccountingQueries queries, CancellationToken ct) =>
        {
            var p = await db.Parties.SingleOrDefaultAsync(x => x.Id == id, ct) ?? throw new NotFoundException("Cari");
            var athletes = await db.Athletes.Where(a => a.PartyId == id).OrderBy(a => a.FirstName).ToListAsync(ct);
            return Results.Ok(new
            {
                party = new
                {
                    p.Id, p.Code, p.Name, kind = AppDbContext.ToSnake(p.Kind.ToString()), p.Phone, p.Email, p.TaxNumber,
                    p.Notes, p.IsActive,
                },
                athletes = athletes.Select(a => new { a.Id, a.FirstName, a.LastName, a.GroupName, a.BirthYear, a.IsActive }),
                summary = await queries.SummaryAsync(id, ct),
            });
        });

        g.MapPost("", async (PartyInput input, AppDbContext db, TenantContext tenant, ICurrentUser user,
            AuditWriter audit, CancellationToken ct) =>
        {
            var p = new Party { TenantId = tenant.Require(), CreatedByUserId = user.UserId };
            p.Code = string.IsNullOrWhiteSpace(input.Code)
                ? await PartyService.NextCodeAsync(db, "C", ct)
                : input.Code.Trim().ToUpperInvariant();
            PartyService.Apply(p, input);
            db.Parties.Add(p);
            audit.Add("party.create", "party", p.Id, new { p.Code, kind = p.Kind.ToString() });
            await db.SaveChangesAsync(ct);
            return Results.Ok(new { p.Id, p.Code });
        });

        g.MapPut("/{id:guid}", async (Guid id, PartyInput input, AppDbContext db, AuditWriter audit, CancellationToken ct) =>
        {
            var p = await db.Parties.SingleOrDefaultAsync(x => x.Id == id, ct) ?? throw new NotFoundException("Cari");
            if (!string.IsNullOrWhiteSpace(input.Code)) p.Code = input.Code.Trim().ToUpperInvariant();
            PartyService.Apply(p, input);
            audit.Add("party.update", "party", p.Id, new { p.Code, kind = p.Kind.ToString(), p.IsActive });
            await db.SaveChangesAsync(ct);
            return Results.Ok(new { ok = true });
        });

        g.MapPost("/{id:guid}/athletes", async (Guid id, AthleteInput input, AppDbContext db, TenantContext tenant,
            ICurrentUser user, AuditWriter audit, CancellationToken ct) =>
        {
            _ = await db.Parties.SingleOrDefaultAsync(x => x.Id == id, ct) ?? throw new NotFoundException("Cari");
            var a = new Athlete { TenantId = tenant.Require(), CreatedByUserId = user.UserId, PartyId = id };
            ApplyAthlete(a, input);
            db.Athletes.Add(a);
            audit.Add("athlete.create", "athlete", a.Id, new { partyId = id });
            await db.SaveChangesAsync(ct);
            return Results.Ok(new { a.Id });
        });

        g.MapPut("/{id:guid}/athletes/{athleteId:guid}", async (Guid id, Guid athleteId, AthleteInput input, AppDbContext db,
            AuditWriter audit, CancellationToken ct) =>
        {
            var a = await db.Athletes.SingleOrDefaultAsync(x => x.Id == athleteId && x.PartyId == id, ct)
                    ?? throw new NotFoundException("Sporcu");
            ApplyAthlete(a, input);
            audit.Add("athlete.update", "athlete", a.Id, new { a.IsActive });
            await db.SaveChangesAsync(ct);
            return Results.Ok(new { ok = true });
        });

        // ---- ADR-004 uc gorunum ve cari ekstre ----
        g.MapGet("/{id:guid}/ledger", async (Guid id, AccountingQueries q, CancellationToken ct) =>
            Results.Ok(await q.LedgerAsync(id, ct)));

        g.MapGet("/{id:guid}/lines", async (Guid id, string? view, AccountingQueries q, CancellationToken ct) =>
        {
            var lines = await q.LinesAsync(id, null, ct);
            return Results.Ok(view switch
            {
                "official" => lines.Where(l => l.DocumentedQuantity != 0).ToList(),
                "pending" => lines.Where(l => l.PendingQuantity > 0).ToList(),
                _ => lines,
            });
        });

        g.MapGet("/{id:guid}/payments", async (Guid id, AccountingQueries q, CancellationToken ct) =>
            Results.Ok(await q.PaymentsAsync(id, ct)));

        g.MapGet("/{id:guid}/documents", async (Guid id, AccountingQueries q, CancellationToken ct) =>
            Results.Ok(await q.DocumentsAsync(id, ct)));
    }

    private static void ApplyAthlete(Athlete a, AthleteInput input)
    {
        a.FirstName = TextRules.CleanName(input.FirstName);
        a.LastName = TextRules.CleanName(input.LastName);
        if (a.FirstName.Length == 0) throw new DomainException("name", "Sporcu adı zorunlu.");
        a.GroupName = string.IsNullOrWhiteSpace(input.GroupName) ? null : TextRules.CleanName(input.GroupName);
        if (input.BirthYear is < 1990 or > 2030) throw new DomainException("birth_year", "Doğum yılı geçersiz.");
        a.BirthYear = input.BirthYear;
        a.IsActive = input.IsActive;
        a.SearchKey = TextRules.SearchKey(a.FirstName, a.LastName);
    }
}
