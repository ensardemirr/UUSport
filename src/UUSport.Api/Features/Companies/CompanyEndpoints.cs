using Microsoft.EntityFrameworkCore;
using UUSport.Api.Data;
using UUSport.Api.Domain;
using UUSport.Api.Features.Audit;
using UUSport.Api.Features.Auth;
using UUSport.Api.Infrastructure;

namespace UUSport.Api.Features.Companies;

public sealed record CompanyInput(string Name, string Code, string? LegalName, string? TaxNumber, string? TaxOffice,
    string? ColorKey, short SortOrder = 0, bool IsActive = true);

/// <summary>
/// Sirketler (ADR-012). Listeyi her personel kendi izinli sirketleri olarak gorur; ekleme/duzenleme
/// yalniz grup yoneticisine aittir. Yeni sirket acilinca varsayilan kasa hesaplari o sirkete yazilir.
/// </summary>
public static class CompanyEndpoints
{
    public static void MapCompanies(this IEndpointRouteBuilder app)
    {
        var g = app.MapGroup("/api/companies");

        g.MapGet("", (CompanyAccess access) => Results.Ok(access.Allowed.Select(a => new
        {
            id = a.TenantId, a.Code, a.Name, a.Role, permissions = a.Permissions.OrderBy(p => p),
            selected = access.Selected.Contains(a.TenantId),
        }))).RequireAuthorization(Policies.Staff);

        // Grup yoneticisi tum sirketleri (pasifler dahil) ayrintisiyla gorur.
        g.MapGet("/all", async (AppDbContext db, CancellationToken ct) =>
            Results.Ok(await db.Tenants.OrderBy(t => t.SortOrder).ThenBy(t => t.CreatedAt)
                .Select(t => new { t.Id, t.Code, t.Name, t.LegalName, t.TaxNumber, t.TaxOffice, t.ColorKey, t.SortOrder, t.IsActive })
                .ToListAsync(ct))).RequireAuthorization(Policies.GroupAdmin);

        g.MapPost("", async (CompanyInput input, AppDbContext db, TenantContext tenant, ICurrentUser user,
            AuditWriter audit, CancellationToken ct) =>
        {
            var t = new Tenant();
            Apply(t, input);
            db.Tenants.Add(t);
            // Yeni sirketin varsayilan hesaplari ve denetim kaydi o sirkete yazilir: yazma baglami gecici
            // olarak yeni sirkete cevrilir (tekil yazma kurali korunur).
            tenant.TenantId = t.Id;
            tenant.ReadTenantIds = [t.Id];
            tenant.TenantCode = t.Code;
            db.CashAccounts.AddRange(
                new CashAccount { TenantId = t.Id, Name = "Merkez Kasa", Kind = CashAccountKind.Cash, CreatedByUserId = user.UserId },
                new CashAccount { TenantId = t.Id, Name = "Banka", Kind = CashAccountKind.Bank, CreatedByUserId = user.UserId },
                new CashAccount { TenantId = t.Id, Name = "POS", Kind = CashAccountKind.Pos, CreatedByUserId = user.UserId });
            audit.Add("company.create", "tenant", t.Id, new { t.Code, t.Name });
            await db.SaveChangesAsync(ct);
            return Results.Ok(new { t.Id, t.Code });
        }).RequireAuthorization(Policies.GroupAdmin);

        g.MapPut("/{id:guid}", async (Guid id, CompanyInput input, AppDbContext db, TenantContext tenant,
            AuditWriter audit, CancellationToken ct) =>
        {
            var t = await db.Tenants.SingleOrDefaultAsync(x => x.Id == id, ct) ?? throw new NotFoundException("Şirket");
            var oldCode = t.Code;
            Apply(t, input);
            if (oldCode != t.Code)
                throw new DomainException("code", "Şirket kodu sonradan değiştirilemez; cari ve ürün kodlarının ön ekidir.");
            if (!t.IsActive && await db.Tenants.CountAsync(x => x.IsActive, ct) == 0)
                throw new DomainException("active", "En az bir aktif şirket kalmalı.");
            // Denetim kaydi ilgili sirkete yazilir.
            tenant.TenantId = t.Id;
            tenant.ReadTenantIds = [t.Id];
            audit.Add("company.update", "tenant", t.Id, new { t.Code, t.Name, t.IsActive });
            await db.SaveChangesAsync(ct);
            return Results.Ok(new { ok = true });
        }).RequireAuthorization(Policies.GroupAdmin);
    }

    private static void Apply(Tenant t, CompanyInput input)
    {
        t.Name = TextRules.CleanName(input.Name);
        if (t.Name.Length == 0) throw new DomainException("name", "Şirket adı zorunlu.");
        t.Code = CompanyCodes.Normalize(input.Code);
        t.LegalName = string.IsNullOrWhiteSpace(input.LegalName) ? null : input.LegalName.Trim();
        t.TaxNumber = string.IsNullOrWhiteSpace(input.TaxNumber) ? null : new string(input.TaxNumber.Where(char.IsAsciiDigit).ToArray());
        t.TaxOffice = string.IsNullOrWhiteSpace(input.TaxOffice) ? null : input.TaxOffice.Trim();
        t.ColorKey = string.IsNullOrWhiteSpace(input.ColorKey) ? null : input.ColorKey.Trim().ToLowerInvariant();
        t.SortOrder = input.SortOrder;
        t.IsActive = input.IsActive;
    }
}
