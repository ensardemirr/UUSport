using System.Globalization;
using Microsoft.EntityFrameworkCore;
using UUSport.Api.Data;
using UUSport.Api.Domain;
using UUSport.Api.Features.Accounting;
using UUSport.Api.Infrastructure;

namespace UUSport.Api.Features.Reports;

/// <summary>Kullanicinin degistirebildigi varsayimlar. Para hesabi sunucuda yapilir (ADR-002 2.5).</summary>
public sealed record ForecastAssumptions(int Months, decimal MonthlyIncome, decimal MonthlyExpense, decimal ReceivableRate, decimal PayableRate);

/// <summary>Bir donem (vadesi gecmis veya bir ay) icin giris/cikis kirilimi ve donem sonu tahmini nakit.</summary>
public sealed record ForecastBucket(
    string Key, string Label, bool IsOverdue,
    decimal ChecksIn, decimal ReceivablesIn, decimal ScenarioIn,
    decimal LoansOut, decimal ChecksOut, decimal PayablesOut, decimal ScenarioOut,
    decimal TotalIn, decimal TotalOut, decimal Net, decimal Closing);

public sealed record ForecastCompany(
    string Company, decimal Opening, decimal OpenReceivables, decimal OpenPayables, decimal CreditCardDebt,
    IReadOnlyList<ForecastBucket> Buckets, string? FirstShortfall);

public sealed record ForecastItem(
    string Company, DateOnly DueDate, string Kind, string Description, string PartyName, decimal Amount, bool IsInflow, bool IsOverdue);

public sealed record ForecastResult(
    DateOnly AsOf, ForecastAssumptions Assumptions, IReadOnlyList<ForecastCompany> Companies, ForecastCompany Total,
    IReadOnlyList<ForecastItem> Items);

/// <summary>
/// Nakit akisi tahmini v0 (M4 on calismasi, 18.10.2026). Yalniz sistemdeki kesin vadeli kalemler kullanilir:
/// kasa/banka/POS bakiyesi (acilis), portfoydeki alinan cekler (giris), odenmemis kredi taksitleri ve verilen
/// cekler (cikis). Vadesi olmayan acik alacak/borc yalniz kullanicinin verdigi oranla ilk aya yazilir; aylik
/// sabit gelir/gider varsayimi yalniz toplam tabloya eklenir (sirketlere dagitilmaz). Vergi, maas, kira, SGK
/// gibi kalemler bu surumde yoktur (ADR-010/011, M4). Yonetim raporudur, resmi beyan degildir.
/// </summary>
public sealed class CashForecastService(AppDbContext db, AccountingQueries queries)
{
    private static readonly CultureInfo Tr = CultureInfo.GetCultureInfo("tr-TR");

    public async Task<ForecastResult> BuildAsync(ForecastAssumptions a, DateOnly today, CancellationToken ct)
    {
        var codes = await queries.CompanyCodesAsync(ct);
        var monthStart = new DateOnly(today.Year, today.Month, 1);
        var horizonEnd = monthStart.AddMonths(a.Months); // haric
        var monthKeys = Enumerable.Range(0, a.Months).Select(i => monthStart.AddMonths(i)).ToList();

        string? BucketOf(DateOnly due) =>
            due < today ? "overdue" : due < horizonEnd ? due.ToString("yyyy-MM", CultureInfo.InvariantCulture) : null;

        // ---- acilis: kasa, banka, POS bakiyeleri (kasa ekranindaki hesapla ayni) ----
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
        var unpaid = installments.Where(i => i.PaymentId is not { } pid || reversedPaymentIds.Contains(pid)).ToList();

        // ---- portfoydeki cekler: alinan = giris, verilen = cikis ----
        var openStatuses = new[] { CheckStatus.InPortfolio, CheckStatus.SubmittedForCollection };
        var checks = await (from c in db.Checks
                            join p in db.Parties on c.PartyId equals p.Id
                            where openStatuses.Contains(c.Status) && c.DueDate < horizonEnd
                            select new { c.TenantId, c.Direction, c.DueDate, c.Amount, c.CheckNo, c.BankName, PartyName = p.Name })
            .ToListAsync(ct);

        var items = new List<ForecastItem>();
        foreach (var i in unpaid)
            items.Add(new ForecastItem(codes.GetValueOrDefault(i.TenantId, ""), i.DueDate, "loan",
                $"{i.LoanName} · {i.InstallmentNo}. taksit", i.PartyName, i.Total, false, i.DueDate < today));
        foreach (var c in checks)
            items.Add(new ForecastItem(codes.GetValueOrDefault(c.TenantId, ""), c.DueDate, c.Direction == CheckDirection.Received ? "check_in" : "check_out",
                $"Çek {c.CheckNo} · {c.BankName}", c.PartyName, c.Amount, c.Direction == CheckDirection.Received, c.DueDate < today));

        // Kapsamdaki sirketler: okunabilen her sirket (RLS); verisi olmayan sirket de sifirlarla listelenir.
        var tenantIds = accounts.Select(x => x.TenantId)
            .Concat(partyBalances.Select(x => x.TenantId)).Concat(unpaid.Select(x => x.TenantId)).Concat(checks.Select(x => x.TenantId))
            .Distinct().OrderBy(id => codes.GetValueOrDefault(id, "")).ToList();

        var companies = new List<ForecastCompany>();
        foreach (var t in tenantIds)
        {
            var opening = accounts.Where(x => x.TenantId == t && x.Kind is CashAccountKind.Cash or CashAccountKind.Bank or CashAccountKind.Pos)
                .Sum(x => accountBalances.GetValueOrDefault(x.Id));
            var cardDebt = -accounts.Where(x => x.TenantId == t && x.Kind == CashAccountKind.CreditCard)
                .Sum(x => Math.Min(0, accountBalances.GetValueOrDefault(x.Id)));
            var receivables = partyBalances.Where(x => x.TenantId == t && x.Kind != PartyKind.Supplier && x.Balance > 0).Sum(x => x.Balance);
            var payables = -partyBalances.Where(x => x.TenantId == t && x.Kind != PartyKind.Customer && x.Balance < 0).Sum(x => x.Balance);
            var code = codes.GetValueOrDefault(t, "");
            var mine = items.Where(x => x.Company == code).ToList();

            var raw = BuildRawBuckets(mine, monthKeys, BucketOf, today);
            // Vadesiz acik alacak/borc: verilen oran kadari ilk ayda tahsil/odenmis varsayilir.
            if (raw.Count > 1)
            {
                raw[1] = raw[1] with
                {
                    ReceivablesIn = Money.Round(receivables * a.ReceivableRate / 100m),
                    PayablesOut = Money.Round(payables * a.PayableRate / 100m),
                };
            }
            companies.Add(Finish(code, opening, receivables, payables, cardDebt, raw));
        }

        // ---- toplam: sirketlerin toplami + aylik varsayim (yalniz burada) ----
        var totalRaw = BuildRawBuckets([], monthKeys, BucketOf, today);
        for (var k = 0; k < totalRaw.Count; k++)
        {
            var b = totalRaw[k];
            var parts = companies.Select(c => c.Buckets[k]).ToList();
            totalRaw[k] = b with
            {
                ChecksIn = parts.Sum(x => x.ChecksIn), ReceivablesIn = parts.Sum(x => x.ReceivablesIn),
                LoansOut = parts.Sum(x => x.LoansOut), ChecksOut = parts.Sum(x => x.ChecksOut), PayablesOut = parts.Sum(x => x.PayablesOut),
                ScenarioIn = b.IsOverdue ? 0 : a.MonthlyIncome, ScenarioOut = b.IsOverdue ? 0 : a.MonthlyExpense,
            };
        }
        var total = Finish("", companies.Sum(c => c.Opening), companies.Sum(c => c.OpenReceivables), companies.Sum(c => c.OpenPayables),
            companies.Sum(c => c.CreditCardDebt), totalRaw);

        return new ForecastResult(today, a, companies, total,
            items.OrderBy(x => x.DueDate).ThenBy(x => x.Company).Take(500).ToList());
    }

    /// <summary>Kalemleri donemlere dagitir (toplamlar ve kapanis henuz hesaplanmamis).</summary>
    private static List<ForecastBucket> BuildRawBuckets(List<ForecastItem> items, List<DateOnly> months,
        Func<DateOnly, string?> bucketOf, DateOnly today)
    {
        var keys = new List<(string Key, string Label, bool Overdue)> { ("overdue", "Vadesi geçmiş", true) };
        keys.AddRange(months.Select(m => (m.ToString("yyyy-MM", CultureInfo.InvariantCulture),
            Tr.TextInfo.ToTitleCase(m.ToString("MMMM yyyy", Tr)), false)));
        return keys.Select(k =>
        {
            var inBucket = items.Where(x => bucketOf(x.DueDate) == k.Key).ToList();
            return new ForecastBucket(k.Key, k.Label, k.Overdue,
                inBucket.Where(x => x.Kind == "check_in").Sum(x => x.Amount), 0, 0,
                inBucket.Where(x => x.Kind == "loan").Sum(x => x.Amount),
                inBucket.Where(x => x.Kind == "check_out").Sum(x => x.Amount), 0, 0, 0, 0, 0, 0);
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
            var tin = b.ChecksIn + b.ReceivablesIn + b.ScenarioIn;
            var tout = b.LoansOut + b.ChecksOut + b.PayablesOut + b.ScenarioOut;
            running += tin - tout;
            if (running < 0 && shortfall is null) shortfall = b.Label;
            done.Add(b with { TotalIn = tin, TotalOut = tout, Net = tin - tout, Closing = running });
        }
        return new ForecastCompany(code, opening, receivables, payables, cardDebt, done, shortfall);
    }
}
