using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;
using UUSport.Api.Features.Imports;

namespace UUSport.Tests;

/// <summary>
/// Ogrenci aktariminda eslesme kurali (guvenlik standardi S-19, X:DATA-01): telefon yoksa/eslesmezse
/// yalniz isme bakarak mevcut cariyle birlestirme YAPILMAZ - iki farkli veli ayni adi tasiyabilir.
/// </summary>
public sealed class ImportMatchingTests
{
    private static byte[] BuildStudentWorkbook(params (string Parent, string? Phone, string Student)[] rows)
    {
        using var wb = new XLWorkbook();
        var ws = wb.AddWorksheet(ImportKinds.StudentSheet);
        var fields = ImportKinds.Fields[ImportKinds.Students];
        for (var c = 0; c < fields.Length; c++) ws.Cell(1, c + 1).Value = fields[c].Key;
        for (var r = 0; r < rows.Length; r++)
        {
            ws.Cell(r + 2, 1).Value = rows[r].Parent;
            ws.Cell(r + 2, 2).Value = rows[r].Phone ?? "";
            ws.Cell(r + 2, 3).Value = rows[r].Student;
        }
        using var ms = new MemoryStream();
        wb.SaveAs(ms);
        return ms.ToArray();
    }

    private static Dictionary<string, int> Mapping()
    {
        var fields = ImportKinds.Fields[ImportKinds.Students];
        return fields.Select((f, i) => (f.Key, i)).ToDictionary(x => x.Key, x => x.i + 1);
    }

    [Fact]
    public async Task TelefonsuzAyniIsimliIkiVeli_AyriCariOlarakEklenir()
    {
        await using var w = await TestWorld.CreateAsync(isAdmin: true);
        // Once telefonlu bir "Ahmet Yilmaz" carisi zaten var olsun.
        await w.PartyAsync("Ahmet Yilmaz");

        var bytes = BuildStudentWorkbook(("Ahmet Yilmaz", null, "Can"));
        var batch = await w.Imports.PreviewAsync(bytes, "test.xlsx", ImportKinds.Students, ImportKinds.StudentSheet, Mapping(), default);
        await w.Imports.ApplyAsync(batch.Id, default);

        var count = await w.Db.Parties.CountAsync(p => p.TenantId == w.Tenant.Require() && p.Name == "Ahmet Yilmaz");
        Assert.Equal(2, count); // mevcut cari + yeni olusan, birlestirilmedi
    }

    [Fact]
    public async Task AyniTelefonluIkiSatir_TekCariyeBaglanir()
    {
        await using var w = await TestWorld.CreateAsync(isAdmin: true);
        var bytes = BuildStudentWorkbook(
            ("Zeynep Kaya", "05321234567", "Ali"),
            ("Zeynep Kaya", "05321234567", "Veli"));
        var batch = await w.Imports.PreviewAsync(bytes, "test.xlsx", ImportKinds.Students, ImportKinds.StudentSheet, Mapping(), default);
        await w.Imports.ApplyAsync(batch.Id, default);

        var count = await w.Db.Parties.CountAsync(p => p.TenantId == w.Tenant.Require() && p.Name == "Zeynep Kaya");
        Assert.Equal(1, count); // ayni telefon: guvenli, tek cariye baglanir
    }
}
