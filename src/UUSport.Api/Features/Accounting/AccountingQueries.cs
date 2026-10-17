using Microsoft.EntityFrameworkCore;
using UUSport.Api.Data;
using UUSport.Api.Domain;
using UUSport.Api.Infrastructure;

namespace UUSport.Api.Features.Accounting;

public sealed record PartySummary(
    Guid PartyId, decimal Balance, decimal DocumentedSalesGross, decimal PendingSalesGross,
    decimal UnallocatedReceipts, int PendingLineCount, int UnallocatedPaymentCount);

public sealed record LedgerRow(
    Guid Id, DateOnly OccurredOn, string SourceType, Guid SourceId, string Description,
    decimal Debit, decimal Credit, decimal Balance, bool IsOfficial);

public sealed record LedgerListRow(
    Guid Id, Guid PartyId, string PartyName, DateOnly OccurredOn, string SourceType, Guid SourceId,
    string Description, decimal Debit, decimal Credit, bool IsOfficial, string Company);

public sealed record LineRow(
    Guid Id, Guid PartyId, string PartyName, DateOnly OccurredOn, string Direction, Guid ItemId, string ItemName, string ItemKind,
    decimal Quantity, decimal UnitPrice, decimal DiscountPercent, decimal VatRate, decimal NetAmount, decimal VatAmount,
    decimal GrossAmount, decimal DocumentedQuantity, decimal PendingQuantity, decimal AllocatedAmount, decimal OpenAmount,
    string Status, Guid? ReversesLineId, string? Description, IReadOnlyList<string> Documents, string Company);

public sealed record PaymentRow(
    Guid Id, Guid PartyId, string PartyName, DateOnly OccurredOn, string Direction, string Method, string AccountName,
    decimal Amount, decimal AllocatedAmount, decimal UnallocatedAmount, string? Reference, string? Description,
    bool IsReversal, bool IsReversed, string Company);

public sealed record DocumentRow(
    Guid Id, Guid PartyId, string PartyName, string Direction, string Type, string DocumentNo, DateOnly DocumentDate,
    decimal NetTotal, decimal VatTotal, decimal GrossTotal, string Status, string? CancelReason, int LinkCount, string Company);

/// <summary>
/// ADR-004 bolum 2.4: uc gorunum ayni veriden hesaplanir, ayri defter tutulmaz.
/// </summary>
public sealed class AccountingQueries(AppDbContext db)
{
    private Dictionary<Guid, string>? _companyCodes;

    /// <summary>
    /// Sirket kimligi -> kisa kod (core.tenant RLS'siz). Birlesik gorunumde her satir hangi sirkete ait oldugunu
    /// tasir (ADR-012 2.3); tek sirkette de dolu gelir, istemci yalniz birden cok sirket seciliyse gosterir.
    /// </summary>
    public async Task<Dictionary<Guid, string>> CompanyCodesAsync(CancellationToken ct) =>
        _companyCodes ??= await db.Tenants.ToDictionaryAsync(t => t.Id, t => t.Code, ct);

    public async Task<PartySummary> SummaryAsync(Guid partyId, CancellationToken ct)
    {
        var balance = await db.LedgerEntries.Where(e => e.PartyId == partyId)
            .SumAsync(e => (decimal?)(e.Debit - e.Credit), ct) ?? 0;
        var lines = await LinesAsync(partyId, null, ct);
        var sales = lines.Where(l => l.Direction == "sale").ToList();
        var documented = sales.Where(l => l.ReversesLineId is null && l.Quantity != 0)
            .Sum(l => l.GrossAmount * l.DocumentedQuantity / l.Quantity);
        var pending = sales.Where(l => l.PendingQuantity > 0)
            .Sum(l => Math.Abs(l.GrossAmount * l.PendingQuantity / l.Quantity));
        var payments = await PaymentsAsync(partyId, ct);
        var unallocated = payments.Where(p => p.Direction == "receipt" && !p.IsReversal && !p.IsReversed)
            .Sum(p => p.UnallocatedAmount);
        return new PartySummary(partyId, balance, Money.Round(documented), Money.Round(pending), unallocated,
            lines.Count(l => l.PendingQuantity > 0),
            payments.Count(p => p.UnallocatedAmount > 0 && !p.IsReversal && !p.IsReversed));
    }

    public async Task<List<LedgerRow>> LedgerAsync(Guid partyId, CancellationToken ct)
    {
        var entries = await db.LedgerEntries.Where(e => e.PartyId == partyId)
            .OrderBy(e => e.OccurredOn).ThenBy(e => e.CreatedAt).ThenBy(e => e.Id)
            .ToListAsync(ct);
        if (entries.Count == 0) return [];

        var officialBySourceId = await BuildOfficialFlagsAsync(partyId, entries, ct);

        decimal running = 0;
        var rows = new List<LedgerRow>(entries.Count);
        foreach (var e in entries)
        {
            running += e.Debit - e.Credit;
            rows.Add(new LedgerRow(e.Id, e.OccurredOn, e.SourceType, e.SourceId, e.Description, e.Debit, e.Credit, running,
                officialBySourceId.GetValueOrDefault(e.SourceId, true)));
        }
        return rows;
    }

    /// <summary>
    /// Tum cariler icin duz hareket listesi (surum 2 hazirlik notu madde: "hesap hareketleri
    /// icin liste"). Tek cariye ait yuruyen bakiye buradaki gibi anlamli olmadigindan Balance
    /// alani yoktur; partyId/tarih araligi ile filtrelenebilir.
    /// </summary>
    public async Task<List<LedgerListRow>> AllLedgerAsync(Guid? partyId, DateOnly? from, DateOnly? to, CancellationToken ct)
    {
        var q = db.LedgerEntries.AsQueryable();
        if (partyId is not null) q = q.Where(e => e.PartyId == partyId);
        if (from is not null) q = q.Where(e => e.OccurredOn >= from);
        if (to is not null) q = q.Where(e => e.OccurredOn <= to);
        var entries = await q.OrderByDescending(e => e.OccurredOn).ThenByDescending(e => e.CreatedAt).ToListAsync(ct);
        if (entries.Count == 0) return [];

        var partyIds = entries.Select(e => e.PartyId).Distinct().ToList();
        var parties = await db.Parties.Where(p => partyIds.Contains(p.Id)).ToDictionaryAsync(p => p.Id, p => p.Name, ct);

        // "Resmi" hesabi cariye ozeldir (LinesAsync partyId ister); her cari icin ayri hesaplanir.
        var officialBySourceId = new Dictionary<Guid, bool>();
        foreach (var pid in partyIds)
        {
            var partyEntries = entries.Where(e => e.PartyId == pid).ToList();
            foreach (var kv in await BuildOfficialFlagsAsync(pid, partyEntries, ct))
                officialBySourceId[kv.Key] = kv.Value;
        }

        var codes = await CompanyCodesAsync(ct);
        return entries.Select(e => new LedgerListRow(e.Id, e.PartyId, parties.GetValueOrDefault(e.PartyId, ""),
            e.OccurredOn, e.SourceType, e.SourceId, e.Description, e.Debit, e.Credit,
            officialBySourceId.GetValueOrDefault(e.SourceId, true), codes.GetValueOrDefault(e.TenantId, ""))).ToList();
    }

    /// <summary>
    /// "Resmi" = muhasebeye gonderilmis kayit (surum 2 hazirlik notu). Satis/alis satiri tam
    /// belgelenmisse (LineRow.Status == "documented") resmidir; tahsilat/odeme, belgelenmis bir
    /// satira dagitilmissa resmidir; iade kayitlari orijinalinin resmi durumunu miras alir;
    /// acilis bakiyesi (muhasebe onayli) ve henuz kullanilmayan onayli duzeltme her zaman resmidir.
    /// </summary>
    private async Task<Dictionary<Guid, bool>> BuildOfficialFlagsAsync(Guid partyId, List<LedgerEntry> entries, CancellationToken ct)
    {
        var result = new Dictionary<Guid, bool>();

        var lineStatuses = (await LinesAsync(partyId, null, ct)).ToDictionary(l => l.Id, l => l.Status == "documented");
        foreach (var e in entries)
        {
            if (e.SourceType is LedgerSources.SalesLine or LedgerSources.PurchaseLine
                or LedgerSources.SalesLineReversal or LedgerSources.PurchaseLineReversal)
                result[e.SourceId] = lineStatuses.GetValueOrDefault(e.SourceId, false);
        }

        var paymentSourceIds = entries.Where(e => e.SourceType is LedgerSources.Payment or LedgerSources.PaymentReversal)
            .Select(e => e.SourceId).Distinct().ToList();
        if (paymentSourceIds.Count > 0)
        {
            var payments = await db.Payments.Where(p => paymentSourceIds.Contains(p.Id))
                .ToDictionaryAsync(p => p.Id, ct);
            // payment_reversal'in kaynak id'si, yeni uretilen ters Payment kaydidir; asil odemenin
            // dagitimlarina bakmak icin ReversesPaymentId uzerinden orijinaline gidilir.
            var effectivePaymentId = paymentSourceIds.ToDictionary(id => id,
                id => payments.TryGetValue(id, out var p) && p.ReversesPaymentId is { } orig ? orig : id);
            var lookupIds = effectivePaymentId.Values.Distinct().ToList();
            var allocations = await db.PaymentAllocations.Where(a => lookupIds.Contains(a.PaymentId) && a.Amount > 0)
                .Select(a => new { a.PaymentId, a.TradeLineId }).ToListAsync(ct);
            var officialByPaymentId = lookupIds.ToDictionary(id => id,
                id => allocations.Any(a => a.PaymentId == id && lineStatuses.GetValueOrDefault(a.TradeLineId, false)));
            foreach (var id in paymentSourceIds)
                result[id] = officialByPaymentId.GetValueOrDefault(effectivePaymentId[id], false);
        }

        return result;
    }

    /// <summary>partyId null ise tum cariler (Muhasebe Bekleyen listesi icin).</summary>
    public async Task<List<LineRow>> LinesAsync(Guid? partyId, TradeDirection? direction, CancellationToken ct)
    {
        var q = db.TradeLines.AsQueryable();
        if (partyId is not null) q = q.Where(l => l.PartyId == partyId);
        if (direction is not null) q = q.Where(l => l.Direction == direction);
        var lines = await q.OrderByDescending(l => l.OccurredOn).ThenByDescending(l => l.CreatedAt).ToListAsync(ct);
        if (lines.Count == 0) return [];

        var ids = lines.Select(l => l.Id).ToList();
        var itemIds = lines.Select(l => l.ItemId).Distinct().ToList();
        var partyIds = lines.Select(l => l.PartyId).Distinct().ToList();
        var items = await db.Items.Where(i => itemIds.Contains(i.Id)).ToDictionaryAsync(i => i.Id, ct);
        var parties = await db.Parties.Where(p => partyIds.Contains(p.Id)).ToDictionaryAsync(p => p.Id, p => p.Name, ct);
        var allocated = await db.PaymentAllocations.Where(a => ids.Contains(a.TradeLineId))
            .GroupBy(a => a.TradeLineId).Select(g => new { g.Key, Sum = g.Sum(a => a.Amount) })
            .ToDictionaryAsync(x => x.Key, x => x.Sum, ct);
        var docs = await (from link in db.DocumentLinks
                          join d in db.Documents on link.DocumentId equals d.Id
                          where link.IsActive && ids.Contains(link.TradeLineId)
                          select new { link.TradeLineId, d.DocumentNo }).ToListAsync(ct);
        var docsByLine = docs.GroupBy(d => d.TradeLineId).ToDictionary(g => g.Key, g => (IReadOnlyList<string>)g.Select(x => x.DocumentNo).Distinct().ToList());

        // Iade miktarlari ve orijinal satirlar (farkli filtrede olsalar bile) hesaba katilir.
        var reversals = await db.TradeLines.Where(l => l.ReversesLineId != null && (ids.Contains(l.ReversesLineId!.Value) || ids.Contains(l.Id)))
            .Select(l => new { l.Id, Original = l.ReversesLineId!.Value, l.Quantity, l.GrossAmount }).ToListAsync(ct);
        var reversedByOriginal = reversals.GroupBy(r => r.Original).ToDictionary(g => g.Key, g => -g.Sum(r => r.Quantity));
        // Iade satirlarinin brutu eksidir; acik tutar net ekonomik tutardan hesaplanir (X:ACC-07).
        var reversedGrossByOriginal = reversals.GroupBy(r => r.Original).ToDictionary(g => g.Key, g => g.Sum(r => r.GrossAmount));
        var originalIds = lines.Where(l => l.ReversesLineId != null).Select(l => l.ReversesLineId!.Value).Distinct().ToList();
        var originals = await db.TradeLines.Where(l => originalIds.Contains(l.Id)).ToDictionaryAsync(l => l.Id, ct);

        var codes = await CompanyCodesAsync(ct);
        var rows = new List<LineRow>(lines.Count);
        foreach (var l in lines)
        {
            decimal pendingQty;
            string status;
            if (l.ReversesLineId is null)
            {
                var effective = l.Quantity - reversedByOriginal.GetValueOrDefault(l.Id);
                pendingQty = Math.Max(0, effective - l.DocumentedQuantity);
                status = effective == 0 ? "reversed"
                    : l.DocumentedQuantity == 0 ? "pending_accounting"
                    : l.DocumentedQuantity >= effective ? "documented"
                    : "partially_documented";
            }
            else
            {
                // Iade belgesi yalniz orijinal satir belgelenmisse beklenir.
                var original = originals.GetValueOrDefault(l.ReversesLineId.Value);
                var needsDocument = original is not null && original.DocumentedQuantity != 0;
                pendingQty = needsDocument ? Math.Max(0, Math.Abs(l.Quantity) - Math.Abs(l.DocumentedQuantity)) : 0;
                status = !needsDocument ? "reversal"
                    : l.DocumentedQuantity == 0 ? "pending_accounting"
                    : Math.Abs(l.DocumentedQuantity) >= Math.Abs(l.Quantity) ? "documented"
                    : "partially_documented";
            }

            var alloc = allocated.GetValueOrDefault(l.Id);
            var item = items[l.ItemId];
            rows.Add(new LineRow(l.Id, l.PartyId, parties.GetValueOrDefault(l.PartyId, ""), l.OccurredOn,
                AppDbContext.ToSnake(l.Direction.ToString()), l.ItemId, item.Name, AppDbContext.ToSnake(item.Kind.ToString()),
                l.Quantity, l.UnitPrice, l.DiscountPercent, l.VatRate, l.NetAmount, l.VatAmount, l.GrossAmount,
                l.DocumentedQuantity, pendingQty, alloc,
                l.ReversesLineId is null ? Math.Max(0, l.GrossAmount + reversedGrossByOriginal.GetValueOrDefault(l.Id) - alloc) : 0,
                status, l.ReversesLineId, l.Description, docsByLine.GetValueOrDefault(l.Id, []),
                codes.GetValueOrDefault(l.TenantId, "")));
        }
        return rows;
    }

    public async Task<List<PaymentRow>> PaymentsAsync(Guid? partyId, CancellationToken ct, DateOnly? from = null, DateOnly? to = null)
    {
        var q = db.Payments.AsQueryable();
        if (partyId is not null) q = q.Where(p => p.PartyId == partyId);
        if (from is not null) q = q.Where(p => p.OccurredOn >= from);
        if (to is not null) q = q.Where(p => p.OccurredOn <= to);
        var payments = await q.OrderByDescending(p => p.OccurredOn).ThenByDescending(p => p.CreatedAt).ToListAsync(ct);
        if (payments.Count == 0) return [];

        var ids = payments.Select(p => p.Id).ToList();
        var allocated = await db.PaymentAllocations.Where(a => ids.Contains(a.PaymentId))
            .GroupBy(a => a.PaymentId).Select(g => new { g.Key, Sum = g.Sum(a => a.Amount) })
            .ToDictionaryAsync(x => x.Key, x => x.Sum, ct);
        var reversed = await db.Payments.Where(p => p.ReversesPaymentId != null && ids.Contains(p.ReversesPaymentId!.Value))
            .Select(p => p.ReversesPaymentId!.Value).ToListAsync(ct);
        var reversedSet = reversed.ToHashSet();
        var accountIds = payments.Select(p => p.CashAccountId).Distinct().ToList();
        var accounts = await db.CashAccounts.Where(a => accountIds.Contains(a.Id)).ToDictionaryAsync(a => a.Id, a => a.Name, ct);
        var partyIds = payments.Select(p => p.PartyId).Distinct().ToList();
        var parties = await db.Parties.Where(p => partyIds.Contains(p.Id)).ToDictionaryAsync(p => p.Id, p => p.Name, ct);

        var codes = await CompanyCodesAsync(ct);
        return payments.Select(p =>
        {
            var alloc = allocated.GetValueOrDefault(p.Id);
            var isReversal = p.ReversesPaymentId is not null;
            var isReversed = reversedSet.Contains(p.Id);
            return new PaymentRow(p.Id, p.PartyId, parties.GetValueOrDefault(p.PartyId, ""), p.OccurredOn,
                AppDbContext.ToSnake(p.Direction.ToString()), AccountingService.MethodName(p.Method),
                accounts.GetValueOrDefault(p.CashAccountId, ""), p.Amount, alloc,
                isReversal || isReversed ? 0 : p.Amount - alloc, p.Reference, p.Description, isReversal, isReversed,
                codes.GetValueOrDefault(p.TenantId, ""));
        }).ToList();
    }

    public async Task<List<DocumentRow>> DocumentsAsync(Guid? partyId, CancellationToken ct)
    {
        var q = db.Documents.AsQueryable();
        if (partyId is not null) q = q.Where(d => d.PartyId == partyId);
        var list = await q.OrderByDescending(d => d.DocumentDate).ThenByDescending(d => d.CreatedAt)
            .Select(d => new { d, LinkCount = d.Links.Count(l => l.IsActive) }).ToListAsync(ct);
        var partyIds = list.Select(x => x.d.PartyId).Distinct().ToList();
        var parties = await db.Parties.Where(p => partyIds.Contains(p.Id)).ToDictionaryAsync(p => p.Id, p => p.Name, ct);
        var codes = await CompanyCodesAsync(ct);
        return list.Select(x => new DocumentRow(x.d.Id, x.d.PartyId, parties.GetValueOrDefault(x.d.PartyId, ""),
            AppDbContext.ToSnake(x.d.Direction.ToString()), AppDbContext.ToSnake(x.d.Type.ToString()), x.d.DocumentNo,
            x.d.DocumentDate, x.d.NetTotal, x.d.VatTotal, x.d.GrossTotal, AppDbContext.ToSnake(x.d.Status.ToString()),
            x.d.CancelReason, x.LinkCount, codes.GetValueOrDefault(x.d.TenantId, ""))).ToList();
    }

    /// <summary>Muhasebe Bekleyen: belgelenmemis satirlar + dagitilmamis tahsilatlar + onay bekleyen acilis bakiyeleri.</summary>
    public async Task<PendingWorkResult> PendingWorkAsync(CancellationToken ct)
    {
        var lines = (await LinesAsync(null, null, ct)).Where(l => l.PendingQuantity > 0).ToList();
        var payments = (await PaymentsAsync(null, ct)).Where(p => p.UnallocatedAmount > 0).ToList();
        var codes = await CompanyCodesAsync(ct);
        var pendingOpenings = await (from o in db.OpeningBalances
                                     join p in db.Parties on o.PartyId equals p.Id
                                     where o.Status == OpeningBalanceStatus.Pending
                                     orderby p.Name
                                     select new { o.Id, o.PartyId, p.Name, o.Amount, o.Note, o.ImportBatchId, o.CreatedAt, o.TenantId })
            .ToListAsync(ct);
        var openings = pendingOpenings.Select(o => new PendingOpeningRow(o.Id, o.PartyId, o.Name, o.Amount, o.Note,
            o.ImportBatchId, o.CreatedAt, codes.GetValueOrDefault(o.TenantId, ""))).ToList();
        return new PendingWorkResult(
            lines.Where(l => l.Direction == "sale").ToList(),
            lines.Where(l => l.Direction == "purchase").ToList(),
            payments,
            openings);
    }
}

public sealed record PendingOpeningRow(Guid Id, Guid PartyId, string PartyName, decimal Amount, string? Note,
    Guid? ImportBatchId, DateTimeOffset CreatedAt, string Company);

public sealed record PendingWorkResult(
    List<LineRow> SalesAwaitingInvoice, List<LineRow> PurchasesAwaitingInvoice,
    List<PaymentRow> UnallocatedPayments, List<PendingOpeningRow> PendingOpeningBalances);
