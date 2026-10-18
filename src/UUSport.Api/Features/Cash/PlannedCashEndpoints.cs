using Microsoft.EntityFrameworkCore;
using UUSport.Api.Data;
using UUSport.Api.Domain;
using UUSport.Api.Features.Accounting;
using UUSport.Api.Features.Audit;
using UUSport.Api.Features.Auth;
using UUSport.Api.Infrastructure;

namespace UUSport.Api.Features.Cash;

public sealed record PlannedCashInput(PlannedCashKind Kind, string Name, decimal Amount, short DayOfMonth, short IntervalMonths,
    DateOnly StartMonth, DateOnly? EndMonth, bool IsActive = true);

/// <summary>
/// Planli nakit kalemleri (maas, SGK, kira, vergi, beklenen aidat). Yalniz nakit akisi tahmininde kullanilir;
/// cariye, kasaya veya deftere yazmaz. Okuma `cash.read`; yazma tek sirket ve `write` (ara katman).
/// </summary>
public static class PlannedCashEndpoints
{
    private static readonly short[] Intervals = [1, 2, 3, 6, 12];

    public static void MapPlannedCash(this IEndpointRouteBuilder app)
    {
        var g = app.MapGroup("/api/planned-cash-items").RequireAuthorization(Policies.Staff).RequirePermission(Permissions.CashRead);

        g.MapGet("", async (AppDbContext db, AccountingQueries q, CancellationToken ct) =>
        {
            var codes = await q.CompanyCodesAsync(ct);
            var list = await db.PlannedCashItems.OrderBy(x => x.Kind).ThenBy(x => x.Name).ToListAsync(ct);
            return Results.Ok(list.Select(x => new
            {
                company = codes.GetValueOrDefault(x.TenantId, ""),
                x.Id, kind = AppDbContext.ToSnake(x.Kind.ToString()), x.Name, x.Amount, x.DayOfMonth, x.IntervalMonths,
                x.StartMonth, x.EndMonth, x.IsActive,
            }));
        });

        g.MapPost("", async (PlannedCashInput input, AppDbContext db, TenantContext tenant, ICurrentUser user,
            AuditWriter audit, CancellationToken ct) =>
        {
            var item = new PlannedCashItem { TenantId = tenant.Require(), CreatedByUserId = user.UserId };
            Apply(item, input);
            db.PlannedCashItems.Add(item);
            audit.Add("planned_cash.create", "planned_cash_item", item.Id, new { kind = item.Kind.ToString(), item.Amount, item.IntervalMonths });
            await db.SaveChangesAsync(ct);
            return Results.Ok(new { item.Id });
        });

        g.MapPut("/{id:guid}", async (Guid id, PlannedCashInput input, AppDbContext db, AuditWriter audit, CancellationToken ct) =>
        {
            var item = await db.PlannedCashItems.SingleOrDefaultAsync(x => x.Id == id, ct) ?? throw new NotFoundException("Planlı kalem");
            Apply(item, input);
            audit.Add("planned_cash.update", "planned_cash_item", item.Id, new { kind = item.Kind.ToString(), item.Amount, item.IsActive });
            await db.SaveChangesAsync(ct);
            return Results.Ok(new { ok = true });
        });
    }

    private static void Apply(PlannedCashItem item, PlannedCashInput input)
    {
        item.Kind = input.Kind;
        item.Name = TextRules.CleanName(input.Name);
        if (item.Name.Length == 0) throw new DomainException("name", "Kalem adı zorunlu.");
        if (input.Amount <= 0) throw new DomainException("amount", "Tutar sıfırdan büyük olmalı.");
        if (input.DayOfMonth is < 1 or > 28) throw new DomainException("day", "Gün 1 ile 28 arasında olmalı.");
        if (!Intervals.Contains(input.IntervalMonths)) throw new DomainException("interval", "Tekrar aylık, 2, 3, 6 veya 12 ayda bir olabilir.");
        var start = new DateOnly(input.StartMonth.Year, input.StartMonth.Month, 1);
        DateOnly? end = input.EndMonth is { } e ? new DateOnly(e.Year, e.Month, 1) : null;
        if (end < start) throw new DomainException("end_month", "Bitiş ayı başlangıçtan önce olamaz.");
        item.Amount = Money.Round(input.Amount);
        item.DayOfMonth = input.DayOfMonth;
        item.IntervalMonths = input.IntervalMonths;
        item.StartMonth = start;
        item.EndMonth = end;
        item.IsActive = input.IsActive;
    }
}
