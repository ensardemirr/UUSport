using Microsoft.EntityFrameworkCore;
using UUSport.Api.Data;
using UUSport.Api.Domain;
using UUSport.Api.Features.Accounting;
using UUSport.Api.Features.Cash;
using UUSport.Api.Infrastructure;

namespace UUSport.Tests;

/// <summary>
/// M0 (docs/reviews/UUSPORT_DANISMA_SENTEZ_2026-09-27.md): dogrulanan hatalarin ve finansal komut
/// butunlugunun testleri. Tum veriler sentetiktir.
/// </summary>
public sealed class FinansalButunlukTests
{
    private static async Task<decimal> OpenAmountAsync(TestWorld w, Guid partyId, Guid lineId) =>
        (await w.Queries.LinesAsync(partyId, null, default)).Single(l => l.Id == lineId).OpenAmount;

    [Fact]
    public async Task IadeSonrasi_AcikTutar_NetTutardanHesaplanir()
    {
        await using var w = await TestWorld.CreateAsync();
        var veli = await w.PartyAsync("Veli");
        var bank = await w.AccountAsync();
        var ders = await w.ItemAsync("Özel ders", 100m, ItemKind.Service, 10);
        var line = await w.Accounting.CreateTradeLineAsync(new TradeLineInput(veli.Id, ders.Id, TradeDirection.Sale, null, 10, null, 0, null), default);
        await w.Accounting.ReverseTradeLineAsync(line.Id, new ReverseLineInput(4, "4 ders iptal", null), default);

        await w.Accounting.CreatePaymentAsync(new PaymentInput(veli.Id, PaymentDirection.Receipt, PaymentMethod.Transfer, bank.Id,
            300m, null, null, null, [new AllocationInput(line.Id, 300m)]), default);
        Assert.Equal(300m, await OpenAmountAsync(w, veli.Id, line.Id)); // 1.000 − 400 iade − 300 dağıtım

        var pay = await w.Accounting.CreatePaymentAsync(new PaymentInput(veli.Id, PaymentDirection.Receipt, PaymentMethod.Transfer, bank.Id,
            400m, null, null, null, null), default);
        var ex = await Assert.ThrowsAsync<DomainException>(() =>
            w.Accounting.AllocateAsync(pay.Id, [new AllocationInput(line.Id, 400m)], default));
        Assert.Equal("allocation", ex.Code);
    }

    [Fact]
    public async Task TamDagitilmisSatirIadeEdilince_FazlaDagitimAvansaDoner()
    {
        await using var w = await TestWorld.CreateAsync();
        var veli = await w.PartyAsync("Veli");
        var bank = await w.AccountAsync();
        var ders = await w.ItemAsync("Özel ders", 100m, ItemKind.Service, 10);
        var line = await w.Accounting.CreateTradeLineAsync(new TradeLineInput(veli.Id, ders.Id, TradeDirection.Sale, null, 10, null, 0, null), default);
        var pay = await w.Accounting.CreatePaymentAsync(new PaymentInput(veli.Id, PaymentDirection.Receipt, PaymentMethod.Transfer, bank.Id,
            1000m, null, null, null, [new AllocationInput(line.Id, 1000m)]), default);

        await w.Accounting.ReverseTradeLineAsync(line.Id, new ReverseLineInput(4, "4 ders iptal", null), default);

        Assert.Equal(-400m, await w.BalanceAsync(veli.Id));
        Assert.Equal(0m, await OpenAmountAsync(w, veli.Id, line.Id));
        var payment = Assert.Single(await w.Queries.PaymentsAsync(veli.Id, default));
        Assert.Equal(400m, payment.UnallocatedAmount);
        Assert.Equal(600m, await w.Db.PaymentAllocations.Where(a => a.PaymentId == pay.Id).SumAsync(a => a.Amount));
    }

    [Fact]
    public async Task TaksitOdemesiIptalEdilince_TaksitYenidenAcilir_VeTekrarOdenebilir()
    {
        await using var w = await TestWorld.CreateAsync();
        var lender = await w.PartyAsync("Örnek Banka", PartyKind.Supplier);
        var bank = await w.AccountAsync();
        var loanAccount = new CashAccount { TenantId = w.Tenant.Require(), Name = "Kredi", Kind = CashAccountKind.Loan, ParentAccountId = bank.Id };
        w.Db.CashAccounts.Add(loanAccount);
        await w.Db.SaveChangesAsync();
        var loan = await w.Loans.CreateLoanAsync(new LoanInput(lender.Id, loanAccount.Id, "Kredi", 2000m, new DateOnly(2026, 1, 1), null,
            [new(1, new DateOnly(2026, 2, 1), 1000m, 50m, 0m), new(2, new DateOnly(2026, 3, 1), 1000m, 40m, 0m)]), default);

        await w.Loans.PayInstallmentAsync(loan.Id, 1, new PayInstallmentInput(bank.Id, PaymentMethod.Transfer, new DateOnly(2026, 2, 3), null), default);
        var first = (await w.Loans.InstallmentsAsync(loan.Id, default)).Single(i => i.InstallmentNo == 1);
        Assert.True(first.IsPaid);
        Assert.Equal(1000m, (await w.Loans.ListAsync(default)).Single().RemainingPrincipal);
        await Assert.ThrowsAsync<DomainException>(() =>
            w.Loans.PayInstallmentAsync(loan.Id, 1, new PayInstallmentInput(bank.Id, PaymentMethod.Transfer, null, null), default));

        await w.Accounting.ReversePaymentAsync(first.PaymentId!.Value, "Yanlış hesaptan ödendi", default);
        Assert.False((await w.Loans.InstallmentsAsync(loan.Id, default)).Single(i => i.InstallmentNo == 1).IsPaid);
        Assert.Equal(2000m, (await w.Loans.ListAsync(default)).Single().RemainingPrincipal);

        await w.Loans.PayInstallmentAsync(loan.Id, 1, new PayInstallmentInput(bank.Id, PaymentMethod.Transfer, new DateOnly(2026, 2, 4), null), default);
        Assert.True((await w.Loans.InstallmentsAsync(loan.Id, default)).Single(i => i.InstallmentNo == 1).IsPaid);
    }

    [Fact]
    public async Task IslemIkinciAdimdaHataVerirse_HicbirFinansalEtkiKalmaz()
    {
        await using var w = await TestWorld.CreateAsync();
        var veli = await w.PartyAsync("Veli");
        var bank = await w.AccountAsync();

        await Assert.ThrowsAsync<InvalidOperationException>(() => w.Accounting.InTransactionAsync(async () =>
        {
            await w.Accounting.CreatePaymentAsync(new PaymentInput(veli.Id, PaymentDirection.Receipt, PaymentMethod.Transfer, bank.Id,
                500m, null, null, null, null), default);
            throw new InvalidOperationException("ikinci adımda hata");
        }, default));

        await using var fresh = TestWorld.CreateContext(new TenantContext { TenantId = w.Tenant.Require() });
        Assert.Equal(0, await fresh.Payments.CountAsync(p => p.PartyId == veli.Id));
        Assert.Equal(0, await fresh.LedgerEntries.CountAsync(e => e.PartyId == veli.Id));
    }

    [Fact]
    public async Task EsZamanliIkiDagitim_AcikTutariAsamaz()
    {
        await using var w = await TestWorld.CreateAsync();
        var veli = await w.PartyAsync("Veli");
        var bank = await w.AccountAsync();
        var ders = await w.ItemAsync("Ders", 100m, ItemKind.Service, 10);
        var line = await w.Accounting.CreateTradeLineAsync(new TradeLineInput(veli.Id, ders.Id, TradeDirection.Sale, null, 1, null, 0, null), default);
        var p1 = await w.Accounting.CreatePaymentAsync(new PaymentInput(veli.Id, PaymentDirection.Receipt, PaymentMethod.Transfer, bank.Id, 80m, null, null, null, null), default);
        var p2 = await w.Accounting.CreatePaymentAsync(new PaymentInput(veli.Id, PaymentDirection.Receipt, PaymentMethod.Transfer, bank.Id, 80m, null, null, null, null), default);

        await using var a = w.As(isAdmin: false);
        await using var b = w.As(isAdmin: false);
        var results = await Task.WhenAll(
            Capture(() => a.Accounting.AllocateAsync(p1.Id, [new AllocationInput(line.Id, 80m)], default)),
            Capture(() => b.Accounting.AllocateAsync(p2.Id, [new AllocationInput(line.Id, 80m)], default)));

        Assert.Equal(1, results.Count(r => r is null));
        Assert.IsType<DomainException>(results.Single(r => r is not null));
        Assert.Equal(80m, await w.Db.PaymentAllocations.Where(x => x.TradeLineId == line.Id).SumAsync(x => x.Amount));
    }

    [Fact]
    public async Task EsZamanliIkiSatis_StokSiniriniAsamaz()
    {
        await using var w = await TestWorld.CreateAsync(isAdmin: false);
        var veli = await w.PartyAsync("Veli");
        var forma = await w.ItemAsync("Forma", 1000m);
        await using (var admin = w.As(isAdmin: true)) await admin.StockInAsync(forma, 1);

        await using var a = w.As(isAdmin: false);
        await using var b = w.As(isAdmin: false);
        var results = await Task.WhenAll(
            Capture(() => a.Accounting.CreateTradeLineAsync(new TradeLineInput(veli.Id, forma.Id, TradeDirection.Sale, null, 1, null, 0, null), default)),
            Capture(() => b.Accounting.CreateTradeLineAsync(new TradeLineInput(veli.Id, forma.Id, TradeDirection.Sale, null, 1, null, 0, null), default)));

        Assert.Equal(1, results.Count(r => r is null));
        Assert.Equal("negative_stock", Assert.IsType<DomainException>(results.Single(r => r is not null)).Code);
        Assert.Equal(0m, await w.Accounting.StockOnHandAsync(forma.Id, default));
    }

    [Theory]
    [InlineData(PaymentDirection.Receipt, PaymentMethod.Card, CashAccountKind.Pos, true)]
    [InlineData(PaymentDirection.Receipt, PaymentMethod.Card, CashAccountKind.CreditCard, false)]
    [InlineData(PaymentDirection.Disbursement, PaymentMethod.Card, CashAccountKind.CreditCard, true)]
    [InlineData(PaymentDirection.Disbursement, PaymentMethod.Card, CashAccountKind.Pos, false)]
    [InlineData(PaymentDirection.Receipt, PaymentMethod.Transfer, CashAccountKind.Bank, true)]
    [InlineData(PaymentDirection.Receipt, PaymentMethod.Cash, CashAccountKind.Bank, false)]
    [InlineData(PaymentDirection.Disbursement, PaymentMethod.Cash, CashAccountKind.Cash, true)]
    public void YonAracHesapMatrisi(PaymentDirection direction, PaymentMethod method, CashAccountKind kind, bool allowed)
    {
        var ex = Record.Exception(() => AccountingService.EnsureMethodMatchesAccount(direction, method, kind));
        if (allowed) Assert.Null(ex);
        else Assert.Equal("account_kind", Assert.IsType<DomainException>(ex).Code);
    }

    [Fact]
    public async Task CekeBagliTahsilat_GenelIptalleGeriAlinamaz_KarsiliksizIleAlinir()
    {
        await using var w = await TestWorld.CreateAsync();
        var veli = await w.PartyAsync("Veli");
        var bank = await w.AccountAsync();
        var check = (await w.Checks.CreateAsync(new CreateChecksInput(veli.Id, CheckDirection.Received, "Akbank",
            [new("000900", new DateOnly(2026, 10, 1), 750m)]), default)).Single();
        await w.Checks.CollectAsync(check.Id, new CollectInput(bank.Id, new DateOnly(2026, 10, 2)), default);
        var paymentId = (await w.Checks.ListAsync(null, default)).Single().PaymentId!.Value;

        var ex = await Assert.ThrowsAsync<DomainException>(() => w.Accounting.ReversePaymentAsync(paymentId, "yanlış", default));
        Assert.Equal("linked_check", ex.Code);

        await w.Checks.BounceAsync(check.Id, new BounceInput("Karşılıksız"), default);
        Assert.Equal(0m, await w.BalanceAsync(veli.Id));
        Assert.Equal("bounced", (await w.Checks.ListAsync(null, default)).Single().Status);
    }

    private static async Task<Exception?> Capture(Func<Task> action)
    {
        try { await action(); return null; }
        catch (Exception ex) { return ex; }
    }
}
