using System.Globalization;
using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using UUSport.Api.Data;
using UUSport.Api.Domain;
using UUSport.Api.Features.Accounting;
using UUSport.Api.Features.Imports;
using UUSport.Api.Infrastructure;

namespace UUSport.Tests;

public sealed class GuvenlikTests
{
    [Fact]
    public async Task RLS_KiraciBaskaKiracininVerisiniGoremez()
    {
        await using var a = await TestWorld.CreateAsync();
        await using var b = await TestWorld.CreateAsync();
        var veli = await a.PartyAsync("A kiracısının velisi");

        Assert.True(await a.Db.Parties.AnyAsync(p => p.Id == veli.Id));
        Assert.False(await b.Db.Parties.AnyAsync(p => p.Id == veli.Id));

        // Baska kiracinin kimligiyle yazma girisimi RLS WITH CHECK ile reddedilir
        b.Db.Parties.Add(new Party { TenantId = a.Tenant.Require(), Code = "X1", Name = "sizma", SearchKey = "sizma" });
        var ex = await Assert.ThrowsAsync<DbUpdateException>(() => b.Db.SaveChangesAsync());
        Assert.Equal("42501", ((PostgresException)ex.InnerException!).SqlState);
    }

    [Fact]
    public async Task KiraciBaglamiYoksa_HicbirSatirGorunmez()
    {
        await using var a = await TestWorld.CreateAsync();
        await a.PartyAsync("Veli");
        await using var none = TestWorld.CreateContext(new TenantContext());
        Assert.Equal(0, await none.Parties.CountAsync());
    }

    [Fact]
    public async Task CariHareketVeDenetimKaydi_Degistirilemez()
    {
        await using var w = await TestWorld.CreateAsync();
        var veli = await w.PartyAsync("Veli");
        var ob = await w.Accounting.CreateOpeningBalanceAsync(veli.Id, 100m, null, default);
        await w.Accounting.DecideOpeningBalanceAsync(ob.Id, true, null, default);

        var ex = await Assert.ThrowsAsync<PostgresException>(() =>
            w.Db.Database.ExecuteSqlRawAsync("UPDATE finance.ledger_entry SET debit = 1"));
        Assert.Equal(PostgresErrorCodes.InsufficientPrivilege, ex.SqlState);
        ex = await Assert.ThrowsAsync<PostgresException>(() =>
            w.Db.Database.ExecuteSqlRawAsync("DELETE FROM audit.audit_event"));
        Assert.Equal(PostgresErrorCodes.InsufficientPrivilege, ex.SqlState);
        Assert.True(await w.Db.AuditEvents.AnyAsync(e => e.Action == "opening_balance.approve"));
    }
}

public sealed class ParaVeMetinKuraliTests
{
    [Theory]
    [InlineData("0.125", "0.13")]
    [InlineData("0.135", "0.14")]
    [InlineData("-0.005", "-0.01")]
    [InlineData("2.675", "2.68")]
    public void Yuvarlama_YarimYukari(string input, string expected) =>
        Assert.Equal(decimal.Parse(expected, CultureInfo.InvariantCulture), Money.Round(decimal.Parse(input, CultureInfo.InvariantCulture)));

    [Fact]
    public void Bolme_KurusFarkiSonParcaya()
    {
        Assert.Equal([33.33m, 33.33m, 33.34m], Money.Split(100m, 3));
        Assert.Equal(100m, Money.Split(100m, 7).Sum());
    }

    [Fact]
    public void SatirTutari_KdvDahil_BrutKesin_KdvSatirdaYuvarlanmaz()
    {
        var a = Money.PriceLine(1, 1000m, true, 0, 20);
        Assert.Equal(1000m, a.Gross);
        Assert.Equal(833.333333m, a.Net);
        Assert.Equal(166.666667m, a.Vat);

        var b = Money.PriceLine(3, 333.33m, false, 0, 10);
        Assert.Equal(999.99m, b.Net);
        Assert.Equal(99.999m, b.Vat);
        Assert.Equal(1099.99m, b.Gross);
    }

    [Fact]
    public void AramaAnahtari_TurkceHarflerdenBagimsiz()
    {
        var k = TextRules.SearchKey("IŞIK Çağrı");
        Assert.Equal(k, TextRules.SearchKey("ışık çağrı"));
        Assert.Equal(k, TextRules.SearchKey("isik  cagri"));
        Assert.Equal("isik cagri", k);
    }

    [Fact]
    public void Eposta_TurkceKulturdeBileAsciiKucukHarf()
    {
        var previous = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = TextRules.Turkish;
            Assert.Equal("info@okul.com", TextRules.NormalizeEmail("INFO@OKUL.COM"));
        }
        finally { CultureInfo.CurrentCulture = previous; }
    }

    [Theory]
    [InlineData("0532 123 45 67", "5321234567")]
    [InlineData("+90 532 123 45 67", "5321234567")]
    [InlineData("5321234567", "5321234567")]
    public void Telefon_Normalize(string input, string expected) => Assert.Equal(expected, TextRules.NormalizePhone(input));

    [Theory]
    [InlineData("1.234,56", "1234.56")]
    [InlineData("1234.56", "1234.56")]
    [InlineData("500", "500")]
    public void Sayi_TurkceVeInvariant(string input, string expected)
    {
        Assert.True(TextRules.TryParseDecimal(input, out var v));
        Assert.Equal(decimal.Parse(expected, CultureInfo.InvariantCulture), v);
    }
}

public sealed class ExcelAktarimTests
{
    /// <summary>Sentetik ogrenci listesi. Gercek veri kullanilmaz.</summary>
    private static byte[] SyntheticWorkbook(string salt)
    {
        using var wb = new XLWorkbook();
        var decoy = wb.AddWorksheet("Sayfa1");
        decoy.Cell(1, 1).Value = "Eski liste";
        decoy.Cell(2, 1).Value = "Birleştirilmemeli";
        var ws = wb.AddWorksheet("Sayfa1 (2)");
        ws.Cell(1, 1).Value = "VELİ";
        ws.Cell(1, 2).Value = "TELEFON";
        ws.Cell(1, 3).Value = "ÖĞRENCİ";
        ws.Cell(1, 4).Value = "SOYAD";
        ws.Cell(1, 5).Value = "BAKİYE";
        object[][] rows =
        [
            ["Örnek Veli Bir", "0532 000 00 01", "Deniz", "Birinci", 1500],
            ["Örnek Veli Bir", "0532 000 00 01", "Ada", "Birinci", "250,50"],
            ["Örnek Veli İki", "", "Ege", "İkinci", 0],
            ["", "0532 000 00 03", "Sahipsiz", "Satır", 100],
            ["Örnek Veli Üç", "0532 000 00 04", "Mert", "Üçüncü", "abc"],
            ["Örnek Veli Bir", "0532 000 00 01", "Deniz", "Birinci", 0],
        ];
        for (var r = 0; r < rows.Length; r++)
            for (var c = 0; c < rows[r].Length; c++)
                ws.Cell(r + 2, c + 1).Value = XLCellValue.FromObject(rows[r][c]);
        wb.AddWorksheet("Sayfa1 (3)").Cell(1, 1).Value = salt;
        using var ms = new MemoryStream();
        wb.SaveAs(ms);
        return ms.ToArray();
    }

    private static readonly Dictionary<string, int> Mapping = new()
    {
        ["veli_adi"] = 1, ["veli_telefon"] = 2, ["ogrenci_adi"] = 3, ["ogrenci_soyadi"] = 4, ["acilis_bakiyesi"] = 5,
    };

    [Fact]
    public async Task OgrenciAktarimi_YalnizSayfa12_Dogrulama_Tekrar_GeriAlma()
    {
        await using var w = await TestWorld.CreateAsync();
        var file = SyntheticWorkbook(Guid.NewGuid().ToString());

        var (sheets, selected) = w.Imports.Inspect(file, ImportKinds.Students, null);
        Assert.Equal(3, sheets.Count);
        Assert.Equal("Sayfa1 (2)", selected!.Name);
        await Assert.ThrowsAsync<DomainException>(async () =>
            await w.Imports.PreviewAsync(file, "liste.xlsx", ImportKinds.Students, "Sayfa1", Mapping, default));

        var batch = await w.Imports.PreviewAsync(file, "liste.xlsx", ImportKinds.Students, "Sayfa1 (2)", Mapping, default);
        Assert.Equal(6, batch.RowsRead);
        Assert.Equal(2, batch.RowsError); // veli adi bos + bakiye sayi degil
        Assert.Equal(0, await w.Db.Parties.CountAsync()); // onizleme ana tabloya yazmaz

        await w.Imports.ApplyAsync(batch.Id, default);
        Assert.Equal(2, await w.Db.Parties.CountAsync());   // Veli Bir (telefonla birlesir), Veli Iki
        Assert.Equal(3, await w.Db.Athletes.CountAsync());  // Deniz tekrari ikinci kez olusmaz
        var ob = await w.Db.OpeningBalances.SingleAsync();
        Assert.Equal(1750.50m, ob.Amount);
        Assert.Equal(OpeningBalanceStatus.Pending, ob.Status);
        Assert.Equal(0, await w.Db.LedgerEntries.CountAsync()); // onaysiz bakiye cariye yazilmaz

        // Ayni dosya ikinci kez uygulanamaz
        await Assert.ThrowsAsync<DomainException>(() =>
            w.Imports.PreviewAsync(file, "liste.xlsx", ImportKinds.Students, "Sayfa1 (2)", Mapping, default));

        await w.Imports.RevertAsync(batch.Id, default);
        Assert.Equal(0, await w.Db.Parties.CountAsync(p => p.IsActive));
        Assert.Equal(OpeningBalanceStatus.Rejected, (await w.Db.OpeningBalances.SingleAsync()).Status);
        Assert.Equal(2, await w.Db.Parties.CountAsync()); // silinmez, pasiflenir
    }

    [Fact]
    public async Task OgrenciAktarimi_Sayfa12YoksaReddedilir()
    {
        await using var w = await TestWorld.CreateAsync();
        using var wb = new XLWorkbook();
        wb.AddWorksheet("Sayfa1").Cell(1, 1).Value = "x";
        using var ms = new MemoryStream();
        wb.SaveAs(ms);
        var ex = Assert.Throws<DomainException>(() => w.Imports.Inspect(ms.ToArray(), ImportKinds.Students, null));
        Assert.Contains("Sayfa1 (2)", ex.Message);
    }

    [Fact]
    public async Task OnayliAcilisBakiyesiVarsa_AktarimGeriAlinamaz()
    {
        await using var w = await TestWorld.CreateAsync();
        var batch = await w.Imports.PreviewAsync(SyntheticWorkbook(Guid.NewGuid().ToString()), "l.xlsx",
            ImportKinds.Students, "Sayfa1 (2)", Mapping, default);
        await w.Imports.ApplyAsync(batch.Id, default);
        var ob = await w.Db.OpeningBalances.SingleAsync();
        await w.Accounting.DecideOpeningBalanceAsync(ob.Id, true, null, default);
        await Assert.ThrowsAsync<DomainException>(() => w.Imports.RevertAsync(batch.Id, default));
    }
}
