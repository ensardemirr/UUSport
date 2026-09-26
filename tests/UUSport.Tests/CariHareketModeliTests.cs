using Microsoft.EntityFrameworkCore;
using Npgsql;
using UUSport.Api.Domain;
using UUSport.Api.Features.Accounting;
using UUSport.Api.Infrastructure;

namespace UUSport.Tests;

/// <summary>ADR-004 senaryolari (S0-S10) ve bolum 4 mutabakat kontrolleri.</summary>
public sealed class CariHareketModeliTests
{
    private static DocumentInput Invoice(Guid partyId, string no, decimal gross, params (Guid line, decimal qty)[] links) =>
        new(partyId, TradeDirection.Sale, DocumentType.EArsiv, no, new DateOnly(2026, 9, 26), gross,
            [new DocumentVatInput(20, Money.Round(gross / 1.2m), gross - Money.Round(gross / 1.2m))],
            links.Select(l => new DocumentLinkInput(l.line, l.qty)).ToList(), null);

    [Fact]
    public async Task S0_KullanicininKabulSenaryosu()
    {
        await using var w = await TestWorld.CreateAsync();
        var veli = await w.PartyAsync("Örnek Veli");
        var forma = await w.ItemAsync("Forma seti", 2500m);
        await w.StockInAsync(forma, 10);
        var bank = await w.AccountAsync();

        // 1) 2.500 TL urun teslimi -> 2.500 borc, stok duser
        var line = await w.Accounting.CreateTradeLineAsync(new TradeLineInput(veli.Id, forma.Id, TradeDirection.Sale, null, 1, null, 0, null), default);
        Assert.Equal(2500m, await w.BalanceAsync(veli.Id));
        Assert.Equal(9m, await w.Accounting.StockOnHandAsync(forma.Id, default));

        // 2) Ayni teslim faturalanir -> borc yine 2.500, stok ikinci kez dusmez
        var doc = await w.Accounting.EnterDocumentAsync(Invoice(veli.Id, "GIB2026000000001", 2500m, (line.Id, 1)), default);
        Assert.Equal(0m, doc.Difference);
        Assert.Equal(2500m, await w.BalanceAsync(veli.Id));
        Assert.Equal(9m, await w.Accounting.StockOnHandAsync(forma.Id, default));

        // 3) 1.000 TL tahsilat -> kalan 1.500
        await w.Accounting.CreatePaymentAsync(new PaymentInput(veli.Id, PaymentDirection.Receipt, PaymentMethod.Transfer, bank.Id, 1000m,
            null, "EFT", null, [new AllocationInput(line.Id, 1000m)]), default);
        Assert.Equal(1500m, await w.BalanceAsync(veli.Id));

        // Fatura cari hareket uretmez: veli icin tam 2 hareket (satis + tahsilat)
        Assert.Equal(2, await w.Db.LedgerEntries.CountAsync(e => e.PartyId == veli.Id));
        var lines = await w.Queries.LinesAsync(veli.Id, null, default);
        Assert.Equal("documented", Assert.Single(lines).Status);
    }

    [Fact]
    public async Task S1_KismiFaturalama()
    {
        await using var w = await TestWorld.CreateAsync();
        var veli = await w.PartyAsync("Veli");
        var ders = await w.ItemAsync("Özel ders", 1000m, ItemKind.Service, 10);
        var line = await w.Accounting.CreateTradeLineAsync(new TradeLineInput(veli.Id, ders.Id, TradeDirection.Sale, null, 10, null, 0, null), default);

        await w.Accounting.EnterDocumentAsync(Invoice(veli.Id, "K-1", 4000m, (line.Id, 4)), default);
        var row = Assert.Single(await w.Queries.LinesAsync(veli.Id, null, default));
        Assert.Equal("partially_documented", row.Status);
        Assert.Equal(6m, row.PendingQuantity);
        Assert.Equal(10000m, await w.BalanceAsync(veli.Id));

        await w.Accounting.EnterDocumentAsync(Invoice(veli.Id, "K-2", 6000m, (line.Id, 6)), default);
        row = Assert.Single(await w.Queries.LinesAsync(veli.Id, null, default));
        Assert.Equal("documented", row.Status);
        Assert.Equal(0m, row.PendingQuantity);

        // Belgelenmemis miktari asan baglanti reddedilir
        await Assert.ThrowsAsync<DomainException>(() =>
            w.Accounting.EnterDocumentAsync(Invoice(veli.Id, "K-3", 1000m, (line.Id, 1)), default));
    }

    [Fact]
    public async Task S2_TopluFatura_ve_S3_FiyatFarkiRaporlanir()
    {
        await using var w = await TestWorld.CreateAsync();
        var veli = await w.PartyAsync("Veli");
        var ders = await w.ItemAsync("Aylık paket", 4000m, ItemKind.Service, 10);
        var a = await w.Accounting.CreateTradeLineAsync(new TradeLineInput(veli.Id, ders.Id, TradeDirection.Sale, new DateOnly(2026, 9, 1), 1, null, 0, null), default);
        var b = await w.Accounting.CreateTradeLineAsync(new TradeLineInput(veli.Id, ders.Id, TradeDirection.Sale, new DateOnly(2026, 9, 15), 1, null, 0, null), default);

        // Portalda 7.999,99 kesildi: yazdigi gibi alinir, fark raporlanir, cari degismez
        var result = await w.Accounting.EnterDocumentAsync(Invoice(veli.Id, "TOPLU-1", 7999.99m, (a.Id, 1), (b.Id, 1)), default);
        Assert.Equal(8000m, result.SystemGross);
        Assert.Equal(-0.01m, result.Difference);
        var doc = await w.Db.Documents.SingleAsync(d => d.Id == result.DocumentId);
        Assert.Equal(7999.99m, doc.GrossTotal);
        Assert.Equal(8000m, await w.BalanceAsync(veli.Id));
    }

    [Fact]
    public async Task S4_Iade_TersSatirlaYapilir_ToplamTamSifirlanir()
    {
        await using var w = await TestWorld.CreateAsync();
        var veli = await w.PartyAsync("Veli");
        var corap = await w.ItemAsync("Futbol çorabı", 333.33m);
        await w.StockInAsync(corap, 5);
        var line = await w.Accounting.CreateTradeLineAsync(new TradeLineInput(veli.Id, corap.Id, TradeDirection.Sale, null, 3, null, 0, null), default);
        Assert.Equal(999.99m, await w.BalanceAsync(veli.Id));

        await w.Accounting.ReverseTradeLineAsync(line.Id, new ReverseLineInput(1, "Beden uymadı", null), default);
        Assert.Equal(666.66m, await w.BalanceAsync(veli.Id));
        Assert.Equal(3m, await w.Accounting.StockOnHandAsync(corap.Id, default));

        await w.Accounting.ReverseTradeLineAsync(line.Id, new ReverseLineInput(2, "Tamamı iade", null), default);
        Assert.Equal(0m, await w.BalanceAsync(veli.Id));
        Assert.Equal(5m, await w.Accounting.StockOnHandAsync(corap.Id, default));
        var sum = await w.Db.TradeLines.Where(l => l.PartyId == veli.Id).SumAsync(l => l.VatAmount);
        Assert.Equal(0m, sum);

        // Ilk satir degismez; fazla iade reddedilir
        Assert.Equal(3m, (await w.Db.TradeLines.SingleAsync(l => l.Id == line.Id)).Quantity);
        await Assert.ThrowsAsync<DomainException>(() =>
            w.Accounting.ReverseTradeLineAsync(line.Id, new ReverseLineInput(1, "fazla", null), default));
    }

    [Fact]
    public async Task S5_FaturaIptali_StokVeCariyiDegistirmez_S6_AyniNumaraTekrarGirilemez()
    {
        await using var w = await TestWorld.CreateAsync();
        var veli = await w.PartyAsync("Veli");
        var forma = await w.ItemAsync("Forma", 2500m);
        await w.StockInAsync(forma, 2);
        var line = await w.Accounting.CreateTradeLineAsync(new TradeLineInput(veli.Id, forma.Id, TradeDirection.Sale, null, 1, null, 0, null), default);
        var doc = await w.Accounting.EnterDocumentAsync(Invoice(veli.Id, "IPT-1", 2500m, (line.Id, 1)), default);

        // S6: ayni numara ikinci kez girilemez (veritabani benzersizlik kisiti)
        var line2 = await w.Accounting.CreateTradeLineAsync(new TradeLineInput(veli.Id, forma.Id, TradeDirection.Sale, null, 1, null, 0, null), default);
        await using (var w2 = w.As(isAdmin: false))
        {
            var ex = await Assert.ThrowsAsync<DbUpdateException>(() =>
                w2.Accounting.EnterDocumentAsync(Invoice(veli.Id, "ipt-1", 2500m, (line2.Id, 1)), default));
            Assert.Equal(PostgresErrorCodes.UniqueViolation, ((PostgresException)ex.InnerException!).SqlState);
        }

        await using var w3 = w.As(isAdmin: false);
        await w3.Accounting.CancelDocumentAsync(doc.DocumentId, "Tutar hatalı girildi", default);
        Assert.Equal(5000m, await w3.BalanceAsync(veli.Id));
        Assert.Equal(0m, await w3.Accounting.StockOnHandAsync(forma.Id, default));
        Assert.Equal(0m, (await w3.Db.TradeLines.SingleAsync(l => l.Id == line.Id)).DocumentedQuantity);

        // Iptalden sonra ayni numara dogru bilgiyle yeniden girilebilir
        await w3.Accounting.EnterDocumentAsync(Invoice(veli.Id, "IPT-1", 2500m, (line.Id, 1)), default);
    }

    [Fact]
    public async Task S8_OnceTahsilat_SonraDagitim_YeniCariHareketUretmez()
    {
        await using var w = await TestWorld.CreateAsync();
        var veli = await w.PartyAsync("Veli");
        var kasa = await w.AccountAsync(CashAccountKind.Cash);
        var paket = await w.ItemAsync("Paket", 3000m, ItemKind.Service, 10);

        var pay = await w.Accounting.CreatePaymentAsync(new PaymentInput(veli.Id, PaymentDirection.Receipt, PaymentMethod.Cash, kasa.Id,
            5000m, null, null, null, null), default);
        Assert.Equal(-5000m, await w.BalanceAsync(veli.Id));

        var line = await w.Accounting.CreateTradeLineAsync(new TradeLineInput(veli.Id, paket.Id, TradeDirection.Sale, null, 1, null, 0, null), default);
        var before = await w.Db.LedgerEntries.CountAsync(e => e.PartyId == veli.Id);
        await w.Accounting.AllocateAsync(pay.Id, [new AllocationInput(line.Id, 3000m)], default);
        Assert.Equal(before, await w.Db.LedgerEntries.CountAsync(e => e.PartyId == veli.Id));
        Assert.Equal(-2000m, await w.BalanceAsync(veli.Id));

        var payments = await w.Queries.PaymentsAsync(veli.Id, default);
        Assert.Equal(2000m, Assert.Single(payments).UnallocatedAmount);

        // Satirin acik tutarini asan dagitim reddedilir
        await Assert.ThrowsAsync<DomainException>(() =>
            w.Accounting.AllocateAsync(pay.Id, [new AllocationInput(line.Id, 1m)], default));
    }

    [Fact]
    public async Task S9_DagitimGeriAlinir_S10_TahsilatIptalEdilir()
    {
        await using var w = await TestWorld.CreateAsync();
        var veli = await w.PartyAsync("Veli");
        var pos = await w.AccountAsync(CashAccountKind.Pos);
        var paket = await w.ItemAsync("Paket", 1200m, ItemKind.Service, 10);
        var line = await w.Accounting.CreateTradeLineAsync(new TradeLineInput(veli.Id, paket.Id, TradeDirection.Sale, null, 1, null, 0, null), default);
        var pay = await w.Accounting.CreatePaymentAsync(new PaymentInput(veli.Id, PaymentDirection.Receipt, PaymentMethod.Card, pos.Id,
            1200m, null, "POS-1", null, [new AllocationInput(line.Id, 1200m)]), default);

        var alloc = await w.Db.PaymentAllocations.SingleAsync(a => a.PaymentId == pay.Id);
        await w.Accounting.ReverseAllocationAsync(alloc.Id, default);
        Assert.Equal(0m, await w.Db.PaymentAllocations.Where(a => a.PaymentId == pay.Id).SumAsync(a => a.Amount));
        Assert.Equal(0m, await w.BalanceAsync(veli.Id));
        await Assert.ThrowsAsync<DomainException>(() => w.Accounting.ReverseAllocationAsync(alloc.Id, default));

        await w.Accounting.ReversePaymentAsync(pay.Id, "Çekim hatalı", default);
        Assert.Equal(1200m, await w.BalanceAsync(veli.Id));
        await Assert.ThrowsAsync<DomainException>(() => w.Accounting.ReversePaymentAsync(pay.Id, "tekrar", default));
    }

    [Fact]
    public async Task IskontoSiniri_ve_EksiStok_YetkiyeBagli()
    {
        await using var w = await TestWorld.CreateAsync(isAdmin: false, maxDiscount: 10);
        var veli = await w.PartyAsync("Veli");
        var forma = await w.ItemAsync("Forma", 1000m);

        var ex = await Assert.ThrowsAsync<DomainException>(() => w.Accounting.CreateTradeLineAsync(
            new TradeLineInput(veli.Id, forma.Id, TradeDirection.Sale, null, 1, null, 15, null), default));
        Assert.Equal("discount_limit", ex.Code);

        ex = await Assert.ThrowsAsync<DomainException>(() => w.Accounting.CreateTradeLineAsync(
            new TradeLineInput(veli.Id, forma.Id, TradeDirection.Sale, null, 1, null, 0, null), default));
        Assert.Equal("negative_stock", ex.Code);

        await using var admin = w.As(isAdmin: true, maxDiscount: 100);
        var line = await admin.Accounting.CreateTradeLineAsync(
            new TradeLineInput(veli.Id, forma.Id, TradeDirection.Sale, null, 1, null, 15, null), default);
        Assert.Equal(850m, line.GrossAmount);
        Assert.Equal(-1m, await admin.Accounting.StockOnHandAsync(forma.Id, default));
    }

    [Fact]
    public async Task AcilisBakiyesi_OnaysizCariyeYazilmaz()
    {
        await using var w = await TestWorld.CreateAsync();
        var veli = await w.PartyAsync("Veli");
        var ob = await w.Accounting.CreateOpeningBalanceAsync(veli.Id, 1750.50m, "Eski sistem", default);
        Assert.Equal(0m, await w.BalanceAsync(veli.Id));
        await w.Accounting.DecideOpeningBalanceAsync(ob.Id, approve: true, null, default);
        Assert.Equal(1750.50m, await w.BalanceAsync(veli.Id));
        await Assert.ThrowsAsync<DomainException>(() => w.Accounting.DecideOpeningBalanceAsync(ob.Id, approve: true, null, default));
    }

    [Fact]
    public async Task Mutabakat_BelgeKaynakliCariHareketVeritabaniTarafindanReddedilir()
    {
        await using var w = await TestWorld.CreateAsync();
        var veli = await w.PartyAsync("Veli");
        w.Db.LedgerEntries.Add(new LedgerEntry
        {
            TenantId = w.Tenant.Require(), PartyId = veli.Id, OccurredOn = new DateOnly(2026, 9, 26),
            SourceType = "document", SourceId = Guid.NewGuid(), Debit = 100, Description = "yasak",
        });
        var ex = await Assert.ThrowsAsync<DbUpdateException>(() => w.Db.SaveChangesAsync());
        Assert.Equal(PostgresErrorCodes.CheckViolation, ((PostgresException)ex.InnerException!).SqlState);
    }

    [Fact]
    public async Task Mutabakat_AyniKaynakIkinciKezCariyeYazamaz()
    {
        await using var w = await TestWorld.CreateAsync();
        var veli = await w.PartyAsync("Veli");
        var source = Guid.NewGuid();
        foreach (var _ in new[] { 1, 2 })
        {
            w.Db.LedgerEntries.Add(new LedgerEntry
            {
                TenantId = w.Tenant.Require(), PartyId = veli.Id, OccurredOn = new DateOnly(2026, 9, 26),
                SourceType = LedgerSources.SalesLine, SourceId = source, Debit = 100, Description = "cift",
            });
        }
        var ex = await Assert.ThrowsAsync<DbUpdateException>(() => w.Db.SaveChangesAsync());
        Assert.Equal(PostgresErrorCodes.UniqueViolation, ((PostgresException)ex.InnerException!).SqlState);
    }
}
