using Microsoft.EntityFrameworkCore;
using UUSport.Api.Data;
using UUSport.Api.Domain;
using UUSport.Api.Features.Auth;
using UUSport.Api.Infrastructure;

namespace UUSport.Api.Features.Accounting;

public sealed record ReasonInput(string Reason);
public sealed record PreviewInput(Guid ItemId, decimal Quantity, decimal? UnitPrice, decimal DiscountPercent);
public sealed record OpeningBalanceInput(Guid PartyId, decimal Amount, string? Note);
public sealed record DecisionInput(bool Approve, string? Note);

public static class AccountingEndpoints
{
    public static void MapAccounting(this IEndpointRouteBuilder app)
    {
        var g = app.MapGroup("/api").RequireAuthorization(Policies.Staff);

        g.MapPost("/trade-lines", async (TradeLineInput input, AccountingService s, CancellationToken ct) =>
        {
            var line = await s.CreateTradeLineAsync(input, ct);
            return Results.Ok(new { line.Id, line.GrossAmount });
        });

        // Istemci para hesabi yapmaz (ADR-002 2.5): onizleme sunucuda hesaplanir, kayit edilmez.
        g.MapPost("/trade-lines/preview", async (PreviewInput input, AppDbContext db, ICurrentUser user, CancellationToken ct) =>
        {
            var item = await db.Items.SingleOrDefaultAsync(i => i.Id == input.ItemId, ct) ?? throw new NotFoundException("Ürün/hizmet");
            var qty = input.Quantity > 0 ? input.Quantity : 0;
            var price = input.UnitPrice ?? item.UnitPrice;
            var disc = Math.Clamp(input.DiscountPercent, 0, 100);
            var a = Money.PriceLine(qty, price, item.PriceIncludesVat, disc, item.VatRate);
            var max = await user.GetMaxDiscountPercentAsync(ct);
            return Results.Ok(new
            {
                net = Money.Round(a.Net), vat = Money.Round(a.Vat), gross = a.Gross, item.VatRate, item.PriceIncludesVat,
                unitPrice = price, maxDiscountPercent = max, discountExceeded = disc > max,
            });
        });

        g.MapPost("/trade-lines/{id:guid}/reverse", async (Guid id, ReverseLineInput input, AccountingService s, CancellationToken ct) =>
        {
            var line = await s.ReverseTradeLineAsync(id, input, ct);
            return Results.Ok(new { line.Id, line.GrossAmount });
        });

        g.MapPost("/payments", async (PaymentInput input, AccountingService s, CancellationToken ct) =>
        {
            var p = await s.CreatePaymentAsync(input, ct);
            return Results.Ok(new { p.Id });
        });

        g.MapPost("/payments/{id:guid}/allocations", async (Guid id, List<AllocationInput> input, AccountingService s, CancellationToken ct) =>
        {
            await s.AllocateAsync(id, input, ct);
            return Results.Ok(new { ok = true });
        });

        g.MapGet("/payments/{id:guid}/allocations", async (Guid id, AppDbContext db, CancellationToken ct) =>
        {
            var rows = await (from a in db.PaymentAllocations
                              join l in db.TradeLines on a.TradeLineId equals l.Id
                              join i in db.Items on l.ItemId equals i.Id
                              where a.PaymentId == id
                              orderby a.CreatedAt
                              select new { a.Id, a.TradeLineId, a.Amount, a.ReversesAllocationId, ItemName = i.Name, l.OccurredOn })
                .ToListAsync(ct);
            var reversed = rows.Where(r => r.ReversesAllocationId != null).Select(r => r.ReversesAllocationId!.Value).ToHashSet();
            return Results.Ok(rows.Select(r => new { r.Id, r.TradeLineId, r.Amount, r.ItemName, r.OccurredOn,
                isReversal = r.ReversesAllocationId != null, isReversed = reversed.Contains(r.Id) }));
        }).RequirePermission(Permissions.LedgerRead);

        g.MapPost("/allocations/{id:guid}/reverse", async (Guid id, AccountingService s, CancellationToken ct) =>
        {
            await s.ReverseAllocationAsync(id, ct);
            return Results.Ok(new { ok = true });
        });

        g.MapPost("/payments/{id:guid}/reverse", async (Guid id, ReasonInput input, AccountingService s, CancellationToken ct) =>
        {
            var p = await s.ReversePaymentAsync(id, input.Reason, ct);
            return Results.Ok(new { p.Id });
        });

        g.MapPost("/documents", async (DocumentInput input, AccountingService s, CancellationToken ct) =>
            Results.Ok(await s.EnterDocumentAsync(input, ct)));

        g.MapGet("/documents", async (Guid? partyId, AccountingQueries q, CancellationToken ct) =>
            Results.Ok(await q.DocumentsAsync(partyId, ct)));

        g.MapGet("/documents/{id:guid}", async (Guid id, AppDbContext db, CancellationToken ct) =>
        {
            var d = await db.Documents.Include(x => x.VatLines).Include(x => x.Links)
                        .SingleOrDefaultAsync(x => x.Id == id, ct) ?? throw new NotFoundException("Fatura");
            var lineIds = d.Links.Select(l => l.TradeLineId).ToList();
            var lines = await (from l in db.TradeLines join i in db.Items on l.ItemId equals i.Id
                               where lineIds.Contains(l.Id)
                               select new { l.Id, ItemName = i.Name, l.Quantity, l.GrossAmount, l.OccurredOn })
                .ToDictionaryAsync(x => x.Id, ct);
            return Results.Ok(new
            {
                d.Id, d.PartyId, direction = AppDbContext.ToSnake(d.Direction.ToString()), type = AppDbContext.ToSnake(d.Type.ToString()),
                d.DocumentNo, d.DocumentDate, d.NetTotal, d.VatTotal, d.GrossTotal,
                status = AppDbContext.ToSnake(d.Status.ToString()), d.Note, d.CancelReason,
                vatLines = d.VatLines.Select(v => new { v.VatRate, v.NetAmount, v.VatAmount }),
                links = d.Links.Select(l => new
                {
                    l.TradeLineId, l.Quantity, l.IsActive,
                    itemName = lines.GetValueOrDefault(l.TradeLineId)?.ItemName,
                    lineQuantity = lines.GetValueOrDefault(l.TradeLineId)?.Quantity,
                    lineGross = lines.GetValueOrDefault(l.TradeLineId)?.GrossAmount,
                }),
            });
        }).RequirePermission(Permissions.DocumentsRead);

        g.MapPost("/documents/{id:guid}/cancel", async (Guid id, ReasonInput input, AccountingService s, CancellationToken ct) =>
        {
            await s.CancelDocumentAsync(id, input.Reason, ct);
            return Results.Ok(new { ok = true });
        }).RequirePermission(Permissions.DocumentsRead);

        g.MapPost("/opening-balances", async (OpeningBalanceInput input, AccountingService s, CancellationToken ct) =>
        {
            var ob = await s.CreateOpeningBalanceAsync(input.PartyId, input.Amount, input.Note, ct);
            return Results.Ok(new { ob.Id });
        });

        g.MapPost("/opening-balances/{id:guid}/decide", async (Guid id, DecisionInput input, AccountingService s, CancellationToken ct) =>
        {
            await s.DecideOpeningBalanceAsync(id, input.Approve, input.Note, ct);
            return Results.Ok(new { ok = true });
        });

        g.MapGet("/pending-work", async (AccountingQueries q, CancellationToken ct) => Results.Ok(await q.PendingWorkAsync(ct)));

        // Hesap hareketleri listesi (surum 2 hazirlik notu): tum cariler, opsiyonel cari/tarih filtresi.
        g.MapGet("/ledger", async (Guid? partyId, DateOnly? from, DateOnly? to, AccountingQueries q, CancellationToken ct) =>
            Results.Ok(await q.AllLedgerAsync(partyId, from, to, ct))).RequirePermission(Permissions.LedgerRead);

        g.MapGet("/dashboard", async (AppDbContext db, AccountingQueries q, TenantContext tenant, CancellationToken ct) =>
        {
            var today = DateOnly.FromDateTime(DateTime.Now);
            var monthStart = new DateOnly(today.Year, today.Month, 1);
            var payments = await q.PaymentsAsync(null, ct, monthStart, today);
            var receiptsThisMonth = payments.Where(p => p.Direction == "receipt").Sum(p => p.IsReversal ? -p.Amount : p.Amount);
            var receiptsToday = payments.Where(p => p.Direction == "receipt" && p.OccurredOn == today).Sum(p => p.IsReversal ? -p.Amount : p.Amount);
            var balanceRows = await db.LedgerEntries.GroupBy(e => new { e.TenantId, e.PartyId })
                .Select(x => new { x.Key.TenantId, Balance = x.Sum(e => e.Debit - e.Credit) }).ToListAsync(ct);
            var balances = balanceRows.Select(b => b.Balance).ToList();
            var codes = await q.CompanyCodesAsync(ct);
            var pending = await q.PendingWorkAsync(ct);
            return Results.Ok(new
            {
                receiptsToday, receiptsThisMonth,
                openReceivables = balances.Where(b => b > 0).Sum(),
                customerAdvances = -balances.Where(b => b < 0).Sum(),
                salesAwaitingInvoice = pending.SalesAwaitingInvoice.Count,
                purchasesAwaitingInvoice = pending.PurchasesAwaitingInvoice.Count,
                unallocatedPayments = pending.UnallocatedPayments.Count,
                pendingOpeningBalances = pending.PendingOpeningBalances.Count,
                recentPayments = payments.Take(8),
                // Birlesik gorunumde sirketler sessizce toplanmaz; her sirketin payi ayri verilir.
                totalsByCompany = codes.Where(c => tenant.ReadTenantIds.Contains(c.Key)).OrderBy(c => c.Value).Select(c => new
                {
                    company = c.Value,
                    receiptsToday = payments.Where(p => p.Company == c.Value && p.Direction == "receipt" && p.OccurredOn == today)
                        .Sum(p => p.IsReversal ? -p.Amount : p.Amount),
                    receiptsThisMonth = payments.Where(p => p.Company == c.Value && p.Direction == "receipt")
                        .Sum(p => p.IsReversal ? -p.Amount : p.Amount),
                    openReceivables = balanceRows.Where(b => b.TenantId == c.Key && b.Balance > 0).Sum(b => b.Balance),
                    customerAdvances = -balanceRows.Where(b => b.TenantId == c.Key && b.Balance < 0).Sum(b => b.Balance),
                }),
            });
        }).RequirePermission(Permissions.LedgerRead);
    }
}
