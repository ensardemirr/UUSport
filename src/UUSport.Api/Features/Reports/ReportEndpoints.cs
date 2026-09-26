using Microsoft.EntityFrameworkCore;
using UUSport.Api.Data;
using UUSport.Api.Domain;
using UUSport.Api.Features.Accounting;
using UUSport.Api.Features.Auth;

namespace UUSport.Api.Features.Reports;

/// <summary>ADR-005 temel raporlari. Rapor verisi yalniz okunur; toplamlar sessizce birlestirilmez.</summary>
public static class ReportEndpoints
{
    public static void MapReports(this IEndpointRouteBuilder app)
    {
        var g = app.MapGroup("/api/reports").RequireAuthorization(Policies.Staff);

        g.MapGet("/daily-collections", async (DateOnly? from, DateOnly? to, AccountingQueries q, CancellationToken ct) =>
        {
            var end = to ?? DateOnly.FromDateTime(DateTime.Now);
            var start = from ?? end.AddDays(-30);
            var payments = (await q.PaymentsAsync(null, ct, start, end)).Where(p => p.Direction == "receipt").ToList();
            var rows = payments
                .GroupBy(p => new { p.OccurredOn, p.Method, p.AccountName })
                .OrderByDescending(x => x.Key.OccurredOn).ThenBy(x => x.Key.Method)
                .Select(x => new
                {
                    date = x.Key.OccurredOn, method = x.Key.Method, account = x.Key.AccountName,
                    count = x.Count(p => !p.IsReversal),
                    gross = x.Where(p => !p.IsReversal).Sum(p => p.Amount),
                    reversed = x.Where(p => p.IsReversal).Sum(p => p.Amount),
                    net = x.Sum(p => p.IsReversal ? -p.Amount : p.Amount),
                }).ToList();
            return Results.Ok(new { from = start, to = end, rows, total = rows.Sum(r => r.net) });
        });

        g.MapGet("/open-balances", async (string? kind, AppDbContext db, CancellationToken ct) =>
        {
            var q = from e in db.LedgerEntries
                    join p in db.Parties on e.PartyId equals p.Id
                    group e by new { p.Id, p.Code, p.Name, p.Kind, p.Phone } into x
                    select new { x.Key.Id, x.Key.Code, x.Key.Name, x.Key.Kind, x.Key.Phone, Balance = x.Sum(e => e.Debit - e.Credit) };
            var list = await q.Where(x => x.Balance != 0).OrderByDescending(x => x.Balance).ToListAsync(ct);
            if (kind == "receivable") list = list.Where(x => x.Balance > 0).ToList();
            if (kind == "payable") list = list.Where(x => x.Balance < 0).ToList();
            return Results.Ok(new
            {
                rows = list.Select(x => new { x.Id, x.Code, x.Name, kind = AppDbContext.ToSnake(x.Kind.ToString()), x.Phone, x.Balance }),
                totalReceivable = list.Where(x => x.Balance > 0).Sum(x => x.Balance),
                totalPayable = -list.Where(x => x.Balance < 0).Sum(x => x.Balance),
            });
        });

        g.MapGet("/uninvoiced", async (string? direction, AccountingQueries q, CancellationToken ct) =>
        {
            var dir = direction == "purchase" ? TradeDirection.Purchase : TradeDirection.Sale;
            var lines = (await q.LinesAsync(null, dir, ct)).Where(l => l.PendingQuantity > 0).ToList();
            return Results.Ok(new
            {
                rows = lines,
                totalPendingGross = lines.Sum(l => Math.Abs(l.GrossAmount * l.PendingQuantity / l.Quantity)),
            });
        });

        g.MapGet("/unallocated", async (AccountingQueries q, CancellationToken ct) =>
        {
            var rows = (await q.PaymentsAsync(null, ct)).Where(p => p.UnallocatedAmount > 0).ToList();
            return Results.Ok(new { rows, total = rows.Sum(r => r.UnallocatedAmount) });
        });

        g.MapGet("/stock", async (AppDbContext db, CancellationToken ct) =>
        {
            var items = await db.Items.Where(i => i.Kind == ItemKind.Stock).OrderBy(i => i.Name).ToListAsync(ct);
            var stock = await db.StockMovements.GroupBy(m => m.ItemId)
                .Select(x => new { x.Key, Qty = x.Sum(m => m.Quantity) }).ToDictionaryAsync(x => x.Key, x => x.Qty, ct);
            return Results.Ok(items.Select(i => new { i.Id, i.Code, i.Name, i.Unit, i.IsActive, stockOnHand = stock.GetValueOrDefault(i.Id) }));
        });

        var audit = app.MapGroup("/api/audit").RequireAuthorization(Policies.Admin);
        audit.MapGet("", async (int? take, string? action, AppDbContext db, CancellationToken ct) =>
        {
            var q = db.AuditEvents.AsQueryable();
            if (!string.IsNullOrWhiteSpace(action)) q = q.Where(a => a.Action.StartsWith(action));
            var rows = await q.OrderByDescending(a => a.OccurredAt).Take(Math.Clamp(take ?? 200, 1, 1000))
                .Select(a => new { a.Id, a.OccurredAt, a.UserName, a.Action, a.TargetType, a.TargetId, a.Summary })
                .ToListAsync(ct);
            return Results.Ok(rows);
        });
    }
}
