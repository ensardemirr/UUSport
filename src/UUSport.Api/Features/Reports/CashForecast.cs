using System.Globalization;
using Microsoft.EntityFrameworkCore;
using UUSport.Api.Data;
using UUSport.Api.Domain;
using UUSport.Api.Features.Accounting;
using UUSport.Api.Infrastructure;

namespace UUSport.Api.Features.Reports;

/// <summary>Kullanicinin her sorguda degistirebildigi varsayimlar (kalici olanlar planli kalemlerdedir).</summary>
public sealed record ForecastAssumptions(int Months, decimal ReceivableRate, decimal PayableRate);

/// <summary>Bir donem (vadesi gecmis veya bir ay) icin giris/cikis kirilimi ve donem sonu tahmini nakit.</summary>
public sealed record ForecastBucket(
    string Key, string Label, bool IsOverdue,
    decimal ChecksIn, decimal ReceivablesIn, decimal PlannedIn,
    decimal LoansOut, decimal ChecksOut, decimal CardOut, decimal PayablesOut,
    decimal SalaryOut, decimal SgkOut, decimal RentOut, decimal VatOut, decimal TaxOut, decimal OtherOut,
    decimal TotalIn, decimal TotalOut, decimal Net, decimal Closing);

public sealed record ForecastCompany(
    string Company, decimal Opening, decimal OpenReceivables, decimal OpenPayables, decimal CreditCardDebt,
    IReadOnlyList<ForecastBucket> Buckets, string? FirstShortfall);

public sealed record ForecastItem(
    string Company, DateOnly DueDate, string Kind, string Description, string PartyName, decimal Amount, bool IsInflow,
    bool IsOverdue, bool IsEstimate);

public sealed record ForecastResult(
    DateOnly AsOf, ForecastAssumptions Assumptions, IReadOnlyList<ForecastCompany> Companies, ForecastCompany Total,
    IReadOnlyList<ForecastItem> Items);

/// <summary>
/// Nakit akisi tahmini (M4 on calismasi, 18.10.2026). Kaynaklar:
/// <list type="bullet">
/// <item>Acilis: kasa, banka ve POS bakiyeleri.</item>
/// <item>Kesin vadeli: portfoydeki alinan/verilen cekler, odenmemis kredi taksitleri.</item>
/// <item>Kredi karti: hesap borcu, kartin son odeme gununde (gun yoksa gelecek ayin 1'i, varsayim).</item>
/// <item>KDV: gecen ay ve bu ayin kayitli faturalarindan (satis KDV'si - alis KDV'si, devreden mahsup), izleyen
/// ayin 28'inde; sonraki aylar icin planli "KDV" kalemi kullanilir. Mali musavir teyidi gerektiren tahmindir.</item>
/// <item>Planli kalemler: maas, SGK, kira, muhtasar, gecici vergi, diger gider/gelir (sirket basina).</item>
/// <item>Vadesiz acik alacak/borc: kullanicinin verdigi oranla ilk aya.</item>
/// </list>
/// Gecmis gunlere dusen planli kalemler odenmis sayilir (odeme takibi yok). Yonetim raporudur, resmi beyan degildir.
/// </summary>
public sealed class CashForecastService(AppDbContext db, AccountingQueries queries)
{
    private static readonly CultureInfo Tr = CultureInfo.GetCultureInfo("tr-TR");
    public const int VatPaymentDay = 28;

    private static readonly Dictionary<string, string> PlannedLabel = new()
    {
        ["salary"] = "Maaş", ["sgk"] = "SGK primi", ["rent"] = "Kira", ["vat"] = "KDV (planlı)",
        ["withholding_tax"] = "Muhtasar", ["provisional_tax"] = "Geçici vergi", ["other_tax"] = "Vergi",
        ["other_expense"] = "Gider", ["other_income"] = "Beklenen tahsilat",
    };

    public async Task<ForecastResult> BuildAsync(ForecastAssumptions a, DateOnly today, CancellationToken ct)
    {
        var codes = await queries.CompanyCodesAsync(ct);
        var monthStart = new DateOnly(today.Year, today.Month, 1);
        var horizonEnd = monthStart.AddMonths(a.Months); // haric
        var monthKeys = Enumerable.Range(0, a.Months).Select(i => monthStart.AddMonths(i)).ToList();
        string Code(Guid t) => codes.GetValueOrDefault(t, "");

        // ---- acilis ve kart borcu: hesap bakiyeleri (kasa ekranindaki hesapla ayni) ----
        var accounts = await db.CashAccounts.Where(x => x.IsActive).ToListAsync(ct);
        var accountBalances = await (from p in db.Payments
                                     group p by p.CashAccountId into g
                                     select new
                                     {
                                         g.Key,
                                         In = g.Where(p => (p.Direction == PaymentDirection.Receipt) == (p.ReversesPaymentId == null)).Sum(p => p.Amount),
                                         Out = g.Where(p => (p.Direction == PaymentDirection.Receipt) != (p.ReversesPaymentId == null)).Sum(p => p.Amount),
                                     }).ToDictionaryAsync(x => x.Key, x => x.In - x.Out, ct);

        // ---- vadesiz acik alacak / borc (cari bakiyeleri) ----
        var partyBalances = await (from e in db.LedgerEntries
                                   join p in db.Parties on e.PartyId equals p.Id
                                   group e by new { p.TenantId, p.Kind, p.Id } into g
                                   select new { g.Key.TenantId, g.Key.Kind, Balance = g.Sum(e => e.Debit - e.Credit) }).ToListAsync(ct);

        var items = new List<ForecastItem>();

        // ---- odenmemis kredi taksitleri (iptal edilmis odeme "odenmemis" sayilir) ----
        var installments = await (from i in db.LoanInstallments
                                  join l in db.Loans on i.LoanId equals l.Id
                                  join p in db.Parties on l.PartyId equals p.Id
                                  where l.IsActive && i.DueDate < horizonEnd
                                  select new { i.TenantId, i.DueDate, i.InstallmentNo, i.PaymentId, Total = i.PrincipalAmount + i.InterestAmount + i.FeeAmount, LoanName = l.Name, PartyName = p.Name })
            .ToListAsync(ct);
        var paidIds = installments.Where(i => i.PaymentId != null).Select(i => i.PaymentId!.Value).ToList();
        var reversedPaymentIds = (await db.Payments.Where(p => p.ReversesPaymentId != null && paidIds.Contains(p.ReversesPaymentId!.Value))
            .Select(p => p.ReversesPaymentId!.Value).ToListAsync(ct)).ToHashSet();
        foreach (var i in installments.Where(i => i.PaymentId is not { } pid || reversedPaymentIds.Contains(pid)))
            items.Add(new ForecastItem(Code(i.TenantId), i.DueDate, "loan", $"{i.LoanName} · {i.InstallmentNo}. taksit", i.PartyName,
                i.Total, false, i.DueDate < today, false));

        // ---- portfoydeki cekler: alinan = giris, verilen = cikis ----
        var openStatuses = new[] { CheckStatus.InPortfolio, CheckStatus.SubmittedForCollection };
        var checks = await (from c in db.Checks
                            join p in db.Parties on c.PartyId equals p.Id
                            where openStatuses.Contains(c.Status) && c.DueDate < horizonEnd
                            select new { c.TenantId, c.Direction, c.DueDate, c.Amount, c.CheckNo, c.BankName, PartyName = p.Name })
            .ToListAsync(ct);
        foreach (var c in checks)
            items.Add(new ForecastItem(Code(c.TenantId), c.DueDate, c.Direction == CheckDirection.Received ? "check_in" : "check_out",
                $"Çek {c.CheckNo} · {c.BankName}", c.PartyName, c.Amount, c.Direction == CheckDirection.Received, c.DueDate < today, false));

        // ---- kredi karti borcu: son odeme gununde (gun tanimsizsa gelecek ayin 1'i) ----
        foreach (var card in accounts.Where(x => x.Kind == CashAccountKind.CreditCard))
        {
            var debt = -Math.Min(0, accountBalances.GetValueOrDefault(card.Id));
            if (debt == 0) continue;
            DateOnly due;
            if (card.PaymentDueDay is { } day)
            {
                due = DayIn(monthStart, day);
                if (due < today) due = DayIn(monthStart.AddMonths(1), day);
            }
            else due = monthStart.AddMonths(1);
            if (due < horizonEnd)
                items.Add(new ForecastItem(Code(card.TenantId), due, "card", card.Name + (card.PaymentDueDay is null ? " · son ödeme günü tanımsız" : ""),
                    "", debt, false, false, card.PaymentDueDay is null));
        }

        // ---- KDV: gecen ay ve bu ayin faturalarindan; odeme izleyen ayin 28'i ----
        var prevMonth = monthStart.AddMonths(-1);
        var vatDocs = await db.Documents
            .Where(d => d.Status == DocumentStatus.Issued && d.DocumentDate >= prevMonth && d.DocumentDate < monthStart.AddMonths(1))
            .Select(d => new { d.TenantId, d.Direction, d.DocumentDate, d.VatTotal }).ToListAsync(ct);
        foreach (var t in vatDocs.Select(d => d.TenantId).Distinct())
        {
            decimal Net(DateOnly m) => vatDocs.Where(d => d.TenantId == t && d.DocumentDate >= m && d.DocumentDate < m.AddMonths(1))
                .Sum(d => d.Direction == TradeDirection.Sale ? d.VatTotal : -d.VatTotal);
            var prevNet = Net(prevMonth);
            var carry = prevNet < 0 ? -prevNet : 0; // devreden KDV bu aya mahsup edilir
            var payments = new[] { (Month: prevMonth, Amount: Math.Max(0, prevNet)), (Month: monthStart, Amount: Math.Max(0, Net(monthStart) - carry)) };
            foreach (var (m, amount) in payments)
            {
                var due = DayIn(m.AddMonths(1), VatPaymentDay);
                if (amount <= 0 || due < today || due >= horizonEnd) continue;
                items.Add(new ForecastItem(Code(t), due, "vat_auto", $"KDV · {Tr.TextInfo.ToTitleCase(m.ToString("MMMM yyyy", Tr))} faturalarından",
                    "", Money.Round(amount), false, false, true));
            }
        }

        // ---- planli kalemler (gecmis gunlere dusenler odenmis sayilir) ----
        var autoVatUntil = monthStart.AddMonths(2); // otomatik KDV'nin kapsadigi odeme aylarinda planli KDV atlanir
        var planned = await db.PlannedCashItems.Where(x => x.IsActive).ToListAsync(ct);
        foreach (var p in planned)
        {
            var kind = AppDbContext.ToSnake(p.Kind.ToString());
            for (var m = p.StartMonth; m < horizonEnd && (p.EndMonth is null || m <= p.EndMonth); m = m.AddMonths(p.IntervalMonths))
            {
                var due = DayIn(m, p.DayOfMonth);
                if (due < today) continue;
                if (p.Kind == PlannedCashKind.Vat && due < autoVatUntil) continue;
                items.Add(new ForecastItem(Code(p.TenantId), due, kind, $"{PlannedLabel[kind]} · {p.Name}", "", p.Amount,
                    p.Kind == PlannedCashKind.OtherIncome, false, true));
            }
        }

        // Kapsamdaki sirketler: okunabilen her sirket (RLS); verisi olmayan sirket de listelenir.
        var tenantIds = accounts.Select(x => x.TenantId).Concat(partyBalances.Select(x => x.TenantId))
            .Concat(planned.Select(x => x.TenantId)).Distinct().ToList();
        var tenantCodes = tenantIds.Select(Code).Concat(items.Select(x => x.Company)).Distinct().Order(StringComparer.Ordinal).ToList();

        var companies = new List<ForecastCompany>();
        foreach (var code in tenantCodes)
        {
            var ids = tenantIds.Where(t => Code(t) == code).ToHashSet();
            var opening = accounts.Where(x => ids.Contains(x.TenantId) && x.Kind is CashAccountKind.Cash or CashAccountKind.Bank or CashAccountKind.Pos)
                .Sum(x => accountBalances.GetValueOrDefault(x.Id));
            var cardDebt = -accounts.Where(x => ids.Contains(x.TenantId) && x.Kind == CashAccountKind.CreditCard)
                .Sum(x => Math.Min(0, accountBalances.GetValueOrDefault(x.Id)));
            var receivables = partyBalances.Where(x => ids.Contains(x.TenantId) && x.Kind != PartyKind.Supplier && x.Balance > 0).Sum(x => x.Balance);
            var payables = -partyBalances.Where(x => ids.Contains(x.TenantId) && x.Kind != PartyKind.Customer && x.Balance < 0).Sum(x => x.Balance);

            var raw = BuildRawBuckets(items.Where(x => x.Company == code).ToList(), monthKeys, today, horizonEnd);
            // Vadesiz acik alacak/borc: verilen oran kadari ilk ayda tahsil/odenmis varsayilir.
            raw[1] = raw[1] with
            {
                ReceivablesIn = Money.Round(receivables * a.ReceivableRate / 100m),
                PayablesOut = Money.Round(payables * a.PayableRate / 100m),
            };
            companies.Add(Finish(code, opening, receivables, payables, cardDebt, raw));
        }

        // ---- toplu: sirketlerin toplami (sessiz birlestirme yok; sirket satirlari ayrica donulur) ----
        var totalRaw = BuildRawBuckets([], monthKeys, today, horizonEnd);
        for (var k = 0; k < totalRaw.Count; k++)
        {
            var parts = companies.Select(c => c.Buckets[k]).ToList();
            totalRaw[k] = totalRaw[k] with
            {
                ChecksIn = parts.Sum(x => x.ChecksIn), ReceivablesIn = parts.Sum(x => x.ReceivablesIn), PlannedIn = parts.Sum(x => x.PlannedIn),
                LoansOut = parts.Sum(x => x.LoansOut), ChecksOut = parts.Sum(x => x.ChecksOut), CardOut = parts.Sum(x => x.CardOut),
                PayablesOut = parts.Sum(x => x.PayablesOut), SalaryOut = parts.Sum(x => x.SalaryOut), SgkOut = parts.Sum(x => x.SgkOut),
                RentOut = parts.Sum(x => x.RentOut), VatOut = parts.Sum(x => x.VatOut), TaxOut = parts.Sum(x => x.TaxOut),
                OtherOut = parts.Sum(x => x.OtherOut),
            };
        }
        var total = Finish("", companies.Sum(c => c.Opening), companies.Sum(c => c.OpenReceivables), companies.Sum(c => c.OpenPayables),
            companies.Sum(c => c.CreditCardDebt), totalRaw);

        return new ForecastResult(today, a, companies, total,
            items.OrderBy(x => x.DueDate).ThenBy(x => x.Company, StringComparer.Ordinal).Take(1000).ToList());
    }

    /// <summary>Ayin verilen gunu; ay daha kisaysa son gun.</summary>
    private static DateOnly DayIn(DateOnly month, int day) =>
        new(month.Year, month.Month, Math.Min(day, DateTime.DaysInMonth(month.Year, month.Month)));

    /// <summary>Kalemleri donemlere dagitir (toplamlar ve kapanis henuz hesaplanmamis).</summary>
    private static List<ForecastBucket> BuildRawBuckets(List<ForecastItem> items, List<DateOnly> months, DateOnly today, DateOnly horizonEnd)
    {
        string? BucketOf(DateOnly due) =>
            due < today ? "overdue" : due < horizonEnd ? due.ToString("yyyy-MM", CultureInfo.InvariantCulture) : null;
        var keys = new List<(string Key, string Label, bool Overdue)> { ("overdue", "Vadesi geçmiş", true) };
        keys.AddRange(months.Select(m => (m.ToString("yyyy-MM", CultureInfo.InvariantCulture),
            Tr.TextInfo.ToTitleCase(m.ToString("MMMM yyyy", Tr)), false)));
        return keys.Select(k =>
        {
            var b = items.Where(x => BucketOf(x.DueDate) == k.Key).ToList();
            decimal Sum(params string[] kinds) => b.Where(x => kinds.Contains(x.Kind)).Sum(x => x.Amount);
            return new ForecastBucket(k.Key, k.Label, k.Overdue,
                ChecksIn: Sum("check_in"), ReceivablesIn: 0, PlannedIn: Sum("other_income"),
                LoansOut: Sum("loan"), ChecksOut: Sum("check_out"), CardOut: Sum("card"), PayablesOut: 0,
                SalaryOut: Sum("salary"), SgkOut: Sum("sgk"), RentOut: Sum("rent"), VatOut: Sum("vat", "vat_auto"),
                TaxOut: Sum("withholding_tax", "provisional_tax", "other_tax"), OtherOut: Sum("other_expense"),
                TotalIn: 0, TotalOut: 0, Net: 0, Closing: 0);
        }).ToList();
    }

    /// <summary>Toplam giris/cikis, net ve yuruyen kapanis; ilk eksiye dusulen donem.</summary>
    private static ForecastCompany Finish(string code, decimal opening, decimal receivables, decimal payables, decimal cardDebt,
        List<ForecastBucket> raw)
    {
        var running = opening;
        string? shortfall = null;
        var done = new List<ForecastBucket>(raw.Count);
        foreach (var b in raw)
        {
            var tin = b.ChecksIn + b.ReceivablesIn + b.PlannedIn;
            var tout = b.LoansOut + b.ChecksOut + b.CardOut + b.PayablesOut + b.SalaryOut + b.SgkOut + b.RentOut + b.VatOut + b.TaxOut + b.OtherOut;
            running += tin - tout;
            if (running < 0 && shortfall is null) shortfall = b.Label;
            done.Add(b with { TotalIn = tin, TotalOut = tout, Net = tin - tout, Closing = running });
        }
        return new ForecastCompany(code, opening, receivables, payables, cardDebt, done, shortfall);
    }
}
