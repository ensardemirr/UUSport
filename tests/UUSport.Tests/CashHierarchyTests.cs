using UUSport.Api.Domain;
using UUSport.Api.Features.Accounting;
using UUSport.Api.Features.Catalog;
using UUSport.Api.Infrastructure;

namespace UUSport.Tests;

/// <summary>Surum 2 hazirlik notu madde 3: kredi karti / kredi hesap hiyerarsisi.</summary>
public sealed class CashHierarchyServiceTests
{
    [Fact]
    public async Task KrediKartiVeKredi_BankaAltindaOlmak_Zorunda()
    {
        await using var w = await TestWorld.CreateAsync();
        var bank = await w.AccountAsync(CashAccountKind.Bank);
        var pos = await w.AccountAsync(CashAccountKind.Pos);

        // Banka olmayan bir hesap altina baglanamaz.
        var ex = await Assert.ThrowsAsync<DomainException>(() =>
            CatalogEndpoints.ValidateHierarchy(w.Db, CashAccountKind.CreditCard, pos.Id, default));
        Assert.Equal("parent", ex.Code);

        // Ust hesap yoksa da reddedilir.
        ex = await Assert.ThrowsAsync<DomainException>(() =>
            CatalogEndpoints.ValidateHierarchy(w.Db, CashAccountKind.Loan, null, default));
        Assert.Equal("parent", ex.Code);

        // Dogru: banka altinda kredi karti kabul edilir.
        var parentId = await CatalogEndpoints.ValidateHierarchy(w.Db, CashAccountKind.CreditCard, bank.Id, default);
        Assert.Equal(bank.Id, parentId);

        // Cash/Bank/Pos ust hesap tasiyamaz.
        ex = await Assert.ThrowsAsync<DomainException>(() =>
            CatalogEndpoints.ValidateHierarchy(w.Db, CashAccountKind.Bank, bank.Id, default));
        Assert.Equal("parent", ex.Code);

        // Bir hesap kendi altina baglanamaz.
        ex = await Assert.ThrowsAsync<DomainException>(() =>
            CatalogEndpoints.ValidateHierarchy(w.Db, CashAccountKind.Loan, bank.Id, default, selfId: bank.Id));
        Assert.Equal("parent", ex.Code);
    }

    [Fact]
    public async Task SirketKrediKarti_OdemeAraci_Card_CreditCardHesabinaYazilabilir()
    {
        await using var w = await TestWorld.CreateAsync();
        var bank = await w.AccountAsync(CashAccountKind.Bank);
        var card = new CashAccount { TenantId = w.Tenant.Require(), Name = "Şirket Kredi Kartı", Kind = CashAccountKind.CreditCard, ParentAccountId = bank.Id };
        w.Db.CashAccounts.Add(card);
        await w.Db.SaveChangesAsync();

        var supplier = await w.PartyAsync("Örnek Tedarikçi", PartyKind.Supplier);
        var payment = await w.Accounting.CreatePaymentAsync(new PaymentInput(
            supplier.Id, PaymentDirection.Disbursement, PaymentMethod.Card, card.Id, 500m, null, null, null, null), default);
        Assert.NotEqual(Guid.Empty, payment.Id);
    }
}
