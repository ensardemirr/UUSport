using UUSport.Api.Domain;
using UUSport.Api.Features.Cash;
using UUSport.Api.Infrastructure;

namespace UUSport.Tests;

/// <summary>Surum 2 hazirlik notu madde 3: cek portfoyu ve seri cek girisi.</summary>
public sealed class CheckServiceTests
{
    [Fact]
    public void SeriOnizleme_ArdisikNumaralarUretir_GenislikKorunur()
    {
        var s = new CheckService(null!, null!, null!, null!, null!);
        var rows = s.PreviewSeries(new CheckSeriesPreviewInput("AB000123", 3, 1000m, new DateOnly(2026, 10, 1), 30));

        Assert.Equal(3, rows.Count);
        Assert.Equal("AB000123", rows[0].CheckNo);
        Assert.Equal("AB000124", rows[1].CheckNo);
        Assert.Equal("AB000125", rows[2].CheckNo);
        Assert.Equal(new DateOnly(2026, 10, 1), rows[0].DueDate);
        Assert.Equal(new DateOnly(2026, 10, 31), rows[1].DueDate);
        Assert.Equal(new DateOnly(2026, 11, 30), rows[2].DueDate);
        Assert.All(rows, r => Assert.Equal(1000m, r.Amount));
    }

    [Fact]
    public void SeriOnizleme_SayisalOlmayanNumarayaIzinVermez()
    {
        var s = new CheckService(null!, null!, null!, null!, null!);
        var ex = Assert.Throws<DomainException>(() => s.PreviewSeries(new CheckSeriesPreviewInput("ABCDEF", 3, 100m, new DateOnly(2026, 1, 1), 0)));
        Assert.Equal("check_no", ex.Code);
    }

    [Fact]
    public async Task TekilCekGirisi_AyniIstektekiTekrarNumarayiReddeder()
    {
        await using var w = await TestWorld.CreateAsync();
        var veli = await w.PartyAsync("Örnek Veli");
        var ex = await Assert.ThrowsAsync<DomainException>(() => w.Checks.CreateAsync(new CreateChecksInput(
            veli.Id, CheckDirection.Received, "Ziraat Bankası",
            [new("000100", new DateOnly(2026, 10, 1), 500m), new("000100", new DateOnly(2026, 11, 1), 500m)]), default));
        Assert.Equal("check_no", ex.Code);
    }

    [Fact]
    public async Task SeriCekGirisi_HepsiAyniBatchIdIleKaydedilir()
    {
        await using var w = await TestWorld.CreateAsync();
        var veli = await w.PartyAsync("Örnek Veli");
        var preview = w.Checks.PreviewSeries(new CheckSeriesPreviewInput("000200", 3, 750m, new DateOnly(2026, 10, 1), 30));
        var checks = await w.Checks.CreateAsync(new CreateChecksInput(veli.Id, CheckDirection.Received, "Garanti",
            preview.Select(p => new CheckRowInput(p.CheckNo, p.DueDate, p.Amount)).ToList()), default);

        Assert.Equal(3, checks.Count);
        Assert.True(checks.Select(c => c.BatchId).Distinct().Count() == 1);
        Assert.All(checks, c => Assert.Equal(CheckStatus.InPortfolio, c.Status));
    }

    [Fact]
    public async Task Tahsil_MusteriCekiIcin_OtomatikTahsilatUretir_GerceklesmeTarihindeSayilir()
    {
        await using var w = await TestWorld.CreateAsync();
        var veli = await w.PartyAsync("Örnek Veli");
        var bank = await w.AccountAsync(CashAccountKind.Bank);
        var checks = await w.Checks.CreateAsync(new CreateChecksInput(veli.Id, CheckDirection.Received, "Akbank",
            [new("000300", new DateOnly(2026, 10, 15), 2500m), new("000301", new DateOnly(2026, 10, 15), 1000m)]), default);
        var check = checks[0];

        await w.Checks.SubmitForCollectionAsync(check.Id, default);
        // Gecikmeli tahsil: vade 15.10, banka 01.11'de gecirdi. Gerceklesen tarih vade degil islem tarihidir (X:CSH-06).
        await w.Checks.CollectAsync(check.Id, new CollectInput(bank.Id, new DateOnly(2026, 11, 1)), default);

        // Onceden borc yoktu; tahsilat Receipt oldugu icin bakiye avans gibi eksiye doner (S8 ile ayni kural).
        Assert.Equal(-2500m, await w.BalanceAsync(veli.Id));
        var ledger = await w.Queries.LedgerAsync(veli.Id, default);
        var entry = Assert.Single(ledger);
        Assert.Equal(new DateOnly(2026, 11, 1), entry.OccurredOn);
        Assert.Equal(2500m, entry.Credit);

        // Tarih verilmezse vade degil bugun kullanilir.
        await w.Checks.CollectAsync(checks[1].Id, new CollectInput(bank.Id, null), default);
        var second = (await w.Queries.LedgerAsync(veli.Id, default)).Single(e => e.Credit == 1000m);
        Assert.Equal(DateOnly.FromDateTime(DateTime.Now), second.OccurredOn);

        var rows = await w.Checks.ListAsync(null, default);
        var row = rows.Single(r => r.Id == check.Id);
        Assert.Equal("collected", row.Status);
        Assert.NotNull(row.PaymentId);

        // Zaten tahsil edilmis cek tekrar tahsil edilemez.
        var ex = await Assert.ThrowsAsync<DomainException>(() => w.Checks.CollectAsync(check.Id, new CollectInput(bank.Id, null), default));
        Assert.Equal("status", ex.Code);
    }

    [Fact]
    public async Task Karsiliksiz_TahsilEdilmisCekinOdemesiniTersKayitlaGeriAlir()
    {
        await using var w = await TestWorld.CreateAsync();
        var veli = await w.PartyAsync("Örnek Veli");
        var bank = await w.AccountAsync(CashAccountKind.Bank);
        var checks = await w.Checks.CreateAsync(new CreateChecksInput(veli.Id, CheckDirection.Received, "Akbank",
            [new("000400", new DateOnly(2026, 10, 15), 1000m)]), default);
        await w.Checks.CollectAsync(checks[0].Id, new CollectInput(bank.Id, null), default);
        Assert.Single(await w.Queries.LedgerAsync(veli.Id, default)); // sadece tahsilat kaydi

        await w.Checks.BounceAsync(checks[0].Id, new BounceInput("Karşılıksız çıktı"), default);

        var ledger = await w.Queries.LedgerAsync(veli.Id, default);
        Assert.Equal(0m, ledger.Sum(l => l.Debit - l.Credit)); // ters kayit net sifirlar
        var rows = await w.Checks.ListAsync(null, default);
        Assert.Equal("bounced", rows.Single(r => r.Id == checks[0].Id).Status);
    }

    [Fact]
    public async Task Karsiliksiz_PortfoydekiCekIcinOdemeUretmedenIsaretlenir()
    {
        await using var w = await TestWorld.CreateAsync();
        var veli = await w.PartyAsync("Örnek Veli");
        var checks = await w.Checks.CreateAsync(new CreateChecksInput(veli.Id, CheckDirection.Received, "Akbank",
            [new("000500", new DateOnly(2026, 10, 15), 1000m)]), default);
        await w.Checks.BounceAsync(checks[0].Id, new BounceInput("Karşılıksız"), default);
        Assert.Empty(await w.Queries.LedgerAsync(veli.Id, default)); // hicbir zaman tahsilat olusmadigi icin ters kayit da yok
    }

    [Fact]
    public async Task Ciro_DurumDegistirir_OdemeUretmez()
    {
        await using var w = await TestWorld.CreateAsync();
        var tedarikci = await w.PartyAsync("Örnek Tedarikçi", PartyKind.Supplier);
        var checks = await w.Checks.CreateAsync(new CreateChecksInput(tedarikci.Id, CheckDirection.Received, "Akbank",
            [new("000600", new DateOnly(2026, 10, 15), 1000m)]), default);
        await w.Checks.EndorseAsync(checks[0].Id, new EndorseInput("Tedarikçiye ciro edildi"), default);
        var rows = await w.Checks.ListAsync(null, default);
        Assert.Equal("endorsed", rows.Single(r => r.Id == checks[0].Id).Status);
        Assert.Empty(await w.Queries.LedgerAsync(tedarikci.Id, default));
    }
}
