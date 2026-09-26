using Microsoft.EntityFrameworkCore;
using UUSport.Api.Domain;
using UUSport.Api.Features.Cash;
using UUSport.Api.Infrastructure;

namespace UUSport.Tests;

/// <summary>Surum 2 hazirlik notu madde 3: kredi ve taksit tablosu.</summary>
public sealed class LoanServiceTests
{
    private static async Task<(Party Lender, Guid LoanAccountId, CashAccount Bank)> Setup(TestWorld w)
    {
        var bank = await w.AccountAsync(CashAccountKind.Bank);
        var loanAccount = new CashAccount { TenantId = w.Tenant.Require(), Name = "Kredi", Kind = CashAccountKind.Loan, ParentAccountId = bank.Id };
        w.Db.CashAccounts.Add(loanAccount);
        await w.Db.SaveChangesAsync();
        var lender = await w.PartyAsync("Örnek Banka", PartyKind.Supplier);
        return (lender, loanAccount.Id, bank);
    }

    [Fact]
    public async Task TaksitToplami_AnaParayaEsitOlmali()
    {
        await using var w = await TestWorld.CreateAsync();
        var (lender, loanAccountId, _) = await Setup(w);

        var ex = await Assert.ThrowsAsync<DomainException>(() => w.Loans.CreateLoanAsync(new LoanInput(
            lender.Id, loanAccountId, "Yatırım Kredisi", 12000m, new DateOnly(2026, 1, 1), null,
            [new LoanInstallmentInput(1, new DateOnly(2026, 2, 1), 999m, 100m, 0m)]), default));
        Assert.Equal("principal_mismatch", ex.Code);
    }

    [Fact]
    public async Task Kredi_BankaOlmayanCariyeVeyaBankaOlmayanHesabaAcilamaz()
    {
        await using var w = await TestWorld.CreateAsync();
        var (lender, loanAccountId, bank) = await Setup(w);
        var installments = new List<LoanInstallmentInput> { new(1, new DateOnly(2026, 2, 1), 1000m, 0m, 0m) };

        var customer = await w.PartyAsync("Örnek Veli"); // Customer, tedarikci degil
        var ex = await Assert.ThrowsAsync<DomainException>(() =>
            w.Loans.CreateLoanAsync(new LoanInput(customer.Id, loanAccountId, "K", 1000m, new DateOnly(2026, 1, 1), null, installments), default));
        Assert.Equal("party_kind", ex.Code);

        ex = await Assert.ThrowsAsync<DomainException>(() =>
            w.Loans.CreateLoanAsync(new LoanInput(lender.Id, bank.Id, "K", 1000m, new DateOnly(2026, 1, 1), null, installments), default));
        Assert.Equal("account_kind", ex.Code);
    }

    [Fact]
    public async Task TaksitOdemesi_NormalOdemeUretir_CariBakiyesiVeHesapBakiyesiDogruGuncellenir()
    {
        await using var w = await TestWorld.CreateAsync();
        var (lender, loanAccountId, bank) = await Setup(w);

        var loan = await w.Loans.CreateLoanAsync(new LoanInput(
            lender.Id, loanAccountId, "İşletme Kredisi", 10000m, new DateOnly(2026, 1, 1), "test",
            [
                new(1, new DateOnly(2026, 2, 1), 3333.33m, 100m, 10m),
                new(2, new DateOnly(2026, 3, 1), 3333.33m, 90m, 10m),
                new(3, new DateOnly(2026, 4, 1), 3333.34m, 80m, 10m),
            ]), default);

        // Kredi acildiginda cariye henuz hicbir hareket yazilmaz (yalniz taksit odemesi yazar).
        Assert.Equal(0m, await w.BalanceAsync(lender.Id));

        await w.Loans.PayInstallmentAsync(loan.Id, 1, new PayInstallmentInput(bank.Id, UUSport.Api.Domain.PaymentMethod.Transfer, null, "dekont-1"), default);

        // Taksit odemesi normal bir Disbursement'tir: mevcut Payment kurali geregi (var olan
        // alis/satis akisiyla ayni) odeme cariye "debit" yazar. Kredi acilirken bir "alis"
        // benzeri karsit kayit olmadigi icin bu bakiye "kalan kredi borcu" DEGILDIR; kalan
        // ana para daima loan_installment'tan (asagida) okunur. Bu, surum 2 hazirlik notunun
        // bilinen bir siniridir (ADR-007'de netlesecek).
        Assert.Equal(3443.33m, await w.BalanceAsync(lender.Id));

        var installments = await w.Loans.InstallmentsAsync(loan.Id, default);
        var first = installments.Single(i => i.InstallmentNo == 1);
        Assert.True(first.IsPaid);
        Assert.NotNull(first.PaymentId);
        Assert.Equal(3443.33m, first.TotalAmount);

        // Ayni taksit ikinci kez odenemez.
        var ex = await Assert.ThrowsAsync<DomainException>(() =>
            w.Loans.PayInstallmentAsync(loan.Id, 1, new PayInstallmentInput(bank.Id, UUSport.Api.Domain.PaymentMethod.Transfer, null, null), default));
        Assert.Equal("already_paid", ex.Code);

        var loans = await w.Loans.ListAsync(default);
        var row = loans.Single(l => l.Id == loan.Id);
        Assert.Equal(1, row.PaidCount);
        Assert.Equal(3, row.InstallmentCount);
        Assert.Equal(6666.67m, row.RemainingPrincipal);
        Assert.Equal(new DateOnly(2026, 3, 1), row.NextDueDate);
    }
}
