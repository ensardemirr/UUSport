using UUSport.Api.Domain;
using UUSport.Api.Features.Accounting;
using UUSport.Api.Infrastructure;

namespace UUSport.Tests;

/// <summary>
/// Surum 2 hazirlik notu madde 2 (Ekstre / "Resmi Cari"): LedgerRow.IsOfficial hesaplamasi.
/// "Resmi" = muhasebeye gonderilmis (tam belgelenmis satir, ya da belgelenmis bir satira dagitilmis tahsilat).
/// </summary>
public sealed class LedgerOfficialFlagTests
{
    private static DocumentInput Invoice(Guid partyId, string no, decimal gross, params (Guid line, decimal qty)[] links) =>
        new(partyId, TradeDirection.Sale, DocumentType.EArsiv, no, new DateOnly(2026, 9, 26), gross,
            [new DocumentVatInput(20, Money.Round(gross / 1.2m), gross - Money.Round(gross / 1.2m))],
            links.Select(l => new DocumentLinkInput(l.line, l.qty)).ToList(), null);

    [Fact]
    public async Task BelgelenmemisSatir_GayriResmidir()
    {
        await using var w = await TestWorld.CreateAsync();
        var veli = await w.PartyAsync("Veli");
        var forma = await w.ItemAsync("Forma", 1000m);
        await w.StockInAsync(forma, 5);
        await w.Accounting.CreateTradeLineAsync(new TradeLineInput(veli.Id, forma.Id, TradeDirection.Sale, null, 1, null, 0, null), default);

        var ledger = await w.Queries.LedgerAsync(veli.Id, default);
        var row = Assert.Single(ledger);
        Assert.Equal(LedgerSources.SalesLine, row.SourceType);
        Assert.False(row.IsOfficial);
    }

    [Fact]
    public async Task TamBelgelenmisSatir_Resmidir_KismiBelgeliDegildir()
    {
        await using var w = await TestWorld.CreateAsync();
        var veli = await w.PartyAsync("Veli");
        var ders = await w.ItemAsync("Özel ders", 1000m, ItemKind.Service, 10);
        var line = await w.Accounting.CreateTradeLineAsync(new TradeLineInput(veli.Id, ders.Id, TradeDirection.Sale, null, 10, null, 0, null), default);

        // Once kismi fatura: 4/10 -> henuz "resmi" sayilmaz (tam belgelenmedi)
        await w.Accounting.EnterDocumentAsync(Invoice(veli.Id, "K-1", 4000m, (line.Id, 4)), default);
        var row = Assert.Single(await w.Queries.LedgerAsync(veli.Id, default));
        Assert.False(row.IsOfficial);

        // Kalan da faturalaninca tam belgelenir -> resmi
        await w.Accounting.EnterDocumentAsync(Invoice(veli.Id, "K-2", 6000m, (line.Id, 6)), default);
        row = Assert.Single(await w.Queries.LedgerAsync(veli.Id, default));
        Assert.True(row.IsOfficial);
    }

    [Fact]
    public async Task BelgelenmisSatiraDagitilanTahsilat_Resmidir_DagitilmayanDegildir()
    {
        await using var w = await TestWorld.CreateAsync();
        var veli = await w.PartyAsync("Veli");
        var bank = await w.AccountAsync();
        var forma = await w.ItemAsync("Forma", 2500m);
        await w.StockInAsync(forma, 5);
        var line = await w.Accounting.CreateTradeLineAsync(new TradeLineInput(veli.Id, forma.Id, TradeDirection.Sale, null, 1, null, 0, null), default);
        await w.Accounting.EnterDocumentAsync(Invoice(veli.Id, "GIB-1", 2500m, (line.Id, 1)), default);

        // Faturali satira dagitilan tahsilat resmidir
        await w.Accounting.CreatePaymentAsync(new PaymentInput(veli.Id, PaymentDirection.Receipt, PaymentMethod.Transfer, bank.Id, 2500m,
            null, "EFT", null, [new AllocationInput(line.Id, 2500m)]), default);
        var ledger = await w.Queries.LedgerAsync(veli.Id, default);
        var paymentRow = Assert.Single(ledger, r => r.SourceType == LedgerSources.Payment);
        Assert.True(paymentRow.IsOfficial);

        // Dagitilmayan (avans) tahsilat gayri resmidir
        await w.Accounting.CreatePaymentAsync(new PaymentInput(veli.Id, PaymentDirection.Receipt, PaymentMethod.Transfer, bank.Id, 500m,
            null, "avans", null, null), default);
        ledger = await w.Queries.LedgerAsync(veli.Id, default);
        var advance = Assert.Single(ledger, r => r.SourceType == LedgerSources.Payment && r.Debit == 0 && r.Credit == 500m);
        Assert.False(advance.IsOfficial);
    }

    [Fact]
    public async Task TahsilatIptali_OrijinalinResmiDurumunuMirasAlir()
    {
        await using var w = await TestWorld.CreateAsync();
        var veli = await w.PartyAsync("Veli");
        var pos = await w.AccountAsync(CashAccountKind.Pos);
        var paket = await w.ItemAsync("Paket", 1200m, ItemKind.Service, 10);
        var line = await w.Accounting.CreateTradeLineAsync(new TradeLineInput(veli.Id, paket.Id, TradeDirection.Sale, null, 1, null, 0, null), default);
        await w.Accounting.EnterDocumentAsync(Invoice(veli.Id, "K-1", 1200m, (line.Id, 1)), default);
        var pay = await w.Accounting.CreatePaymentAsync(new PaymentInput(veli.Id, PaymentDirection.Receipt, PaymentMethod.Card, pos.Id,
            1200m, null, "POS-1", null, [new AllocationInput(line.Id, 1200m)]), default);

        await w.Accounting.ReversePaymentAsync(pay.Id, "Çekim hatalı", default);
        var ledger = await w.Queries.LedgerAsync(veli.Id, default);
        var reversalRow = Assert.Single(ledger, r => r.SourceType == LedgerSources.PaymentReversal);
        Assert.True(reversalRow.IsOfficial);
    }

    [Fact]
    public async Task AcilisBakiyesi_HerZamanResmidir()
    {
        await using var w = await TestWorld.CreateAsync();
        var veli = await w.PartyAsync("Veli");
        var ob = await w.Accounting.CreateOpeningBalanceAsync(veli.Id, 1000m, "Eski sistem", default);
        await w.Accounting.DecideOpeningBalanceAsync(ob.Id, approve: true, null, default);

        var row = Assert.Single(await w.Queries.LedgerAsync(veli.Id, default));
        Assert.Equal(LedgerSources.OpeningBalance, row.SourceType);
        Assert.True(row.IsOfficial);
    }
}
