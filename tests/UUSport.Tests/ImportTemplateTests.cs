using UUSport.Api.Features.Imports;

namespace UUSport.Tests;

/// <summary>Surum 2 hazirlik notu madde 1: Excel sablon indirme.</summary>
public sealed class ImportTemplateTests
{
    [Theory]
    [InlineData(ImportKinds.Students)]
    [InlineData(ImportKinds.Parties)]
    [InlineData(ImportKinds.Items)]
    public async Task Sablon_ZorunluAlanlariIsaretler_VeGeriYuklendiginde_Tanınır(string kind)
    {
        await using var w = await TestWorld.CreateAsync();
        var bytes = ImportService.BuildTemplate(kind);
        Assert.True(bytes.Length > 0);

        var (sheets, selected) = w.Imports.Inspect(bytes, kind, null);
        Assert.NotNull(selected);
        var fields = UUSport.Api.Features.Imports.ImportKinds.Fields[kind];
        foreach (var f in fields)
        {
            var expectedHeader = f.Required ? $"{f.Label} *" : f.Label;
            Assert.Contains(selected!.Columns, c => c.Header == expectedHeader);
        }

        if (kind == ImportKinds.Students)
            Assert.Contains(ImportKinds.StudentSheet, sheets);
    }

    [Fact]
    public void OgrenciSablonu_Sayfa12IleAyniAddaOlusturulur()
    {
        var bytes = ImportService.BuildTemplate(ImportKinds.Students);
        using var wb = new ClosedXML.Excel.XLWorkbook(new MemoryStream(bytes));
        Assert.True(wb.Worksheets.Contains(ImportKinds.StudentSheet));
    }
}
