using System.Globalization;
using System.Security.Cryptography;
using System.Text.Json;
using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;
using UUSport.Api.Data;
using UUSport.Api.Domain;
using UUSport.Api.Features.Audit;
using UUSport.Api.Features.Catalog;
using UUSport.Api.Features.Parties;
using UUSport.Api.Infrastructure;

namespace UUSport.Api.Features.Imports;

public sealed record ImportField(string Key, string Label, bool Required);

public static class ImportKinds
{
    public const string Students = "ogrenci";
    public const string Parties = "cari";
    public const string Items = "urun";

    /// <summary>`[KULLANICI KARARI]` Ogrenci aktariminin tek kaynagi bu sayfadir; benzer adli sayfalar birlestirilmez.</summary>
    public const string StudentSheet = "Sayfa1 (2)";

    public static readonly IReadOnlyDictionary<string, ImportField[]> Fields = new Dictionary<string, ImportField[]>
    {
        [Students] =
        [
            new("veli_adi", "Veli adı soyadı", true),
            new("veli_telefon", "Veli telefonu", false),
            new("ogrenci_adi", "Öğrenci adı", true),
            new("ogrenci_soyadi", "Öğrenci soyadı", false),
            new("grup", "Grup / branş", false),
            new("dogum_yili", "Doğum yılı", false),
            new("acilis_bakiyesi", "Açılış bakiyesi (borç +, alacak −)", false),
        ],
        [Parties] =
        [
            new("cari_adi", "Cari adı / unvan", true),
            new("cari_kodu", "Cari kodu", false),
            new("tur", "Tür (müşteri / tedarikçi / ikisi)", false),
            new("telefon", "Telefon", false),
            new("eposta", "E-posta", false),
            new("vergi_no", "Vergi / TC no", false),
            new("acilis_bakiyesi", "Açılış bakiyesi (borç +, alacak −)", false),
        ],
        [Items] =
        [
            new("ad", "Ürün / hizmet adı", true),
            new("kod", "Kod", false),
            new("tur", "Tür (stok / hizmet)", false),
            new("birim", "Birim", false),
            new("birim_fiyat", "Birim fiyat", true),
            new("kdv_orani", "KDV oranı (%)", true),
            new("kdv_dahil", "Fiyat KDV dahil mi (evet / hayır)", false),
        ],
    };

    /// <summary>Sablon 2. satirindaki ornek deger; tamami sentetiktir, gercek veri icermez.</summary>
    public static readonly IReadOnlyDictionary<string, string> ExampleValues = new Dictionary<string, string>
    {
        ["veli_adi"] = "Örnek Veli", ["veli_telefon"] = "0532 000 00 00", ["ogrenci_adi"] = "Örnek",
        ["ogrenci_soyadi"] = "Öğrenci", ["grup"] = "U11", ["dogum_yili"] = "2015", ["acilis_bakiyesi"] = "0",
        ["cari_adi"] = "Örnek Cari Ltd.", ["cari_kodu"] = "", ["tur"] = "müşteri", ["telefon"] = "0532 000 00 00",
        ["eposta"] = "ornek@example.com", ["vergi_no"] = "1234567890",
        ["ad"] = "Örnek ürün", ["kod"] = "", ["birim"] = "adet", ["birim_fiyat"] = "100", ["kdv_orani"] = "20",
        ["kdv_dahil"] = "evet",
    };

    public static readonly IReadOnlyDictionary<string, string> KindTitle = new Dictionary<string, string>
    {
        [Students] = "Öğrenci Listesi", [Parties] = "Cari Listesi", [Items] = "Ürün ve Hizmet Listesi",
    };

    public static void Validate(string kind)
    {
        if (!Fields.ContainsKey(kind)) throw new DomainException("kind", "Geçersiz aktarım türü.");
    }
}

public sealed record SheetInfo(string Name, List<ColumnInfo> Columns, int DataRowCount);
public sealed record ColumnInfo(int Index, string Letter, string Header);

/// <summary>
/// Excel aktarimi (ADR-005 madde 10, operasyon proseduru bolum 2): harici dosya dogrudan ana
/// tablolara yazilmaz; once onizleme ve satir dogrulamasi, sonra onayla uygulama. Ayni dosya
/// ikinci kez uygulanamaz; hatali aktarim silinmez, geri alinir.
/// </summary>
public sealed class ImportService(AppDbContext db, TenantContext tenant, ICurrentUser user, AuditWriter audit)
{
    private const int MaxRows = 20_000;

    public static string Sha256(byte[] bytes) => Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();

    public (List<string> Sheets, SheetInfo? Selected) Inspect(byte[] file, string kind, string? sheetName)
    {
        ImportKinds.Validate(kind);
        using var wb = Open(file);
        var sheets = wb.Worksheets.Select(w => w.Name).ToList();
        var target = ResolveSheetName(sheets, kind, sheetName);
        return (sheets, target is null ? null : Describe(wb.Worksheet(target)));
    }

    public async Task<ImportBatch> PreviewAsync(byte[] file, string fileName, string kind, string sheetName,
        Dictionary<string, int> mapping, CancellationToken ct)
    {
        ImportKinds.Validate(kind);
        var hash = Sha256(file);
        if (await db.ImportBatches.AnyAsync(b => b.Kind == kind && b.FileSha256 == hash && b.Status == ImportStatus.Applied, ct))
            throw new DomainException("duplicate_file", "Bu dosya daha önce aktarıldı. Aynı dosya ikinci kez uygulanamaz.");

        foreach (var f in ImportKinds.Fields[kind].Where(f => f.Required))
            if (!mapping.ContainsKey(f.Key)) throw new DomainException("mapping", $"'{f.Label}' alanı için sütun seçilmeli.");

        using var wb = Open(file);
        var sheets = wb.Worksheets.Select(w => w.Name).ToList();
        var target = ResolveSheetName(sheets, kind, sheetName) ?? throw new DomainException("sheet", "Sayfa bulunamadı.");
        var ws = wb.Worksheet(target);
        var used = ws.RangeUsed() ?? throw new DomainException("empty", "Sayfa boş.");
        var firstRow = used.FirstRow().RowNumber();
        var lastRow = used.LastRow().RowNumber();
        if (lastRow - firstRow > MaxRows) throw new DomainException("too_many", $"En fazla {MaxRows} satır aktarılabilir.");

        var batch = new ImportBatch
        {
            TenantId = tenant.Require(), CreatedByUserId = user.UserId, Kind = kind, FileName = Path.GetFileName(fileName),
            FileSha256 = hash, SheetName = target, MappingJson = JsonSerializer.Serialize(mapping),
        };
        db.ImportBatches.Add(batch);

        var context = await BuildMatchContext(kind, ct);
        var seen = new HashSet<string>(StringComparer.Ordinal);
        for (var r = firstRow + 1; r <= lastRow; r++)
        {
            var row = ws.Row(r);
            var values = mapping.ToDictionary(m => m.Key, m => CellText(row.Cell(m.Value)));
            if (values.Values.All(string.IsNullOrWhiteSpace)) continue;

            var (normalized, errors, warnings) = kind switch
            {
                ImportKinds.Students => ValidateStudent(values, context, seen),
                ImportKinds.Parties => ValidateParty(values, context, seen),
                _ => ValidateItem(values, context, seen),
            };
            batch.RowsRead++;
            if (errors.Count == 0) batch.RowsValid++; else batch.RowsError++;
            db.ImportRows.Add(new ImportRow
            {
                TenantId = batch.TenantId, CreatedByUserId = user.UserId, BatchId = batch.Id, RowNumber = r,
                DataJson = JsonSerializer.Serialize(normalized), IsValid = errors.Count == 0,
                Errors = errors.Count == 0 ? null : string.Join(" ", errors),
                Warnings = warnings.Count == 0 ? null : string.Join(" ", warnings),
            });
        }

        audit.Add("import.preview", "import_batch", batch.Id,
            new { kind, file = batch.FileName, sheet = target, batch.RowsRead, batch.RowsValid, batch.RowsError });
        await db.SaveChangesAsync(ct);
        return batch;
    }

    public async Task ApplyAsync(Guid batchId, CancellationToken ct)
    {
        var batch = await db.ImportBatches.SingleOrDefaultAsync(b => b.Id == batchId, ct) ?? throw new NotFoundException("Aktarım");
        if (batch.Status != ImportStatus.Previewed) throw new DomainException("status", "Bu aktarım zaten uygulanmış veya geri alınmış.");
        if (await db.ImportBatches.AnyAsync(b => b.Kind == batch.Kind && b.FileSha256 == batch.FileSha256 && b.Status == ImportStatus.Applied, ct))
            throw new DomainException("duplicate_file", "Bu dosya daha önce aktarıldı.");

        var rows = await db.ImportRows.Where(r => r.BatchId == batchId && r.IsValid).OrderBy(r => r.RowNumber).ToListAsync(ct);
        if (rows.Count == 0) throw new DomainException("no_rows", "Uygulanacak geçerli satır yok.");

        switch (batch.Kind)
        {
            case ImportKinds.Students: await ApplyStudents(batch, rows, ct); break;
            case ImportKinds.Parties: await ApplyParties(batch, rows, ct); break;
            default: await ApplyItems(batch, rows, ct); break;
        }

        batch.Status = ImportStatus.Applied;
        batch.AppliedAt = DateTimeOffset.UtcNow;
        batch.AppliedByUserId = user.UserId;
        audit.Add("import.apply", "import_batch", batch.Id, new { batch.Kind, rows = rows.Count });
        await db.SaveChangesAsync(ct);
    }

    /// <summary>Geri alma: kayitlar silinmez, kullanim disi birakilir; onay bekleyen acilis bakiyeleri reddedilir.</summary>
    public async Task RevertAsync(Guid batchId, CancellationToken ct)
    {
        var batch = await db.ImportBatches.SingleOrDefaultAsync(b => b.Id == batchId, ct) ?? throw new NotFoundException("Aktarım");
        if (batch.Status != ImportStatus.Applied) throw new DomainException("status", "Yalnız uygulanmış aktarım geri alınabilir.");

        if (await db.OpeningBalances.AnyAsync(o => o.ImportBatchId == batchId && o.Status == OpeningBalanceStatus.Approved, ct))
            throw new DomainException("approved_opening",
                "Bu aktarımdan gelen onaylanmış açılış bakiyesi var. Önce muhasebe ters kaydıyla düzeltilmeli; aktarım geri alınamaz.");
        var createdParties = db.Parties.Where(p => p.ImportBatchId == batchId).Select(p => p.Id);
        if (await db.LedgerEntries.AnyAsync(e => createdParties.Contains(e.PartyId), ct))
            throw new DomainException("has_movements", "Bu aktarımla oluşan carilere hareket girilmiş; aktarım geri alınamaz.");
        var createdItems = db.Items.Where(i => i.ImportBatchId == batchId).Select(i => i.Id);
        if (await db.TradeLines.AnyAsync(l => createdItems.Contains(l.ItemId), ct))
            throw new DomainException("has_movements", "Bu aktarımla oluşan ürünlere hareket girilmiş; aktarım geri alınamaz.");

        await db.Parties.Where(p => p.ImportBatchId == batchId).ForEachAsync(p => p.IsActive = false, ct);
        await db.Athletes.Where(a => a.ImportBatchId == batchId).ForEachAsync(a => a.IsActive = false, ct);
        await db.Items.Where(i => i.ImportBatchId == batchId).ForEachAsync(i => i.IsActive = false, ct);
        await db.OpeningBalances.Where(o => o.ImportBatchId == batchId && o.Status == OpeningBalanceStatus.Pending)
            .ForEachAsync(o => { o.Status = OpeningBalanceStatus.Rejected; o.DecidedAt = DateTimeOffset.UtcNow; o.DecidedByUserId = user.UserId; o.Note = "Aktarım geri alındı"; }, ct);

        batch.Status = ImportStatus.Reverted;
        batch.RevertedAt = DateTimeOffset.UtcNow;
        batch.RevertedByUserId = user.UserId;
        audit.Add("import.revert", "import_batch", batch.Id, new { batch.Kind });
        await db.SaveChangesAsync(ct);
    }

    // ------------------------------------------------------------------ dogrulama

    private sealed class MatchContext
    {
        public Dictionary<string, Party> PartiesByPhone { get; } = new(StringComparer.Ordinal);
        public Dictionary<string, Party> PartiesByName { get; } = new(StringComparer.Ordinal);
        public Dictionary<string, Party> PartiesByCode { get; } = new(StringComparer.Ordinal);
        /// <summary>Kod on eki (ADR-012 2.8): dosyadaki kodlar on eksiz de olsa sistem koduyla eslesir.</summary>
        public string CompanyCode { get; set; } = string.Empty;
        public HashSet<string> ItemCodes { get; } = new(StringComparer.Ordinal);
    }

    private async Task<MatchContext> BuildMatchContext(string kind, CancellationToken ct)
    {
        var ctx = new MatchContext { CompanyCode = await tenant.RequireCodeAsync(db, ct) };
        if (kind == ImportKinds.Items)
        {
            foreach (var code in await db.Items.Select(i => i.Code).ToListAsync(ct)) ctx.ItemCodes.Add(code);
            return ctx;
        }
        foreach (var p in await db.Parties.Where(p => p.IsActive).ToListAsync(ct))
        {
            if (p.Phone is not null) ctx.PartiesByPhone.TryAdd(p.Phone, p);
            ctx.PartiesByName.TryAdd(TextRules.SearchKey(p.Name), p);
            ctx.PartiesByCode.TryAdd(p.Code, p);
        }
        return ctx;
    }

    private static (Dictionary<string, string?>, List<string>, List<string>) ValidateStudent(
        Dictionary<string, string?> v, MatchContext ctx, HashSet<string> seen)
    {
        var errors = new List<string>();
        var warnings = new List<string>();
        var parent = TextRules.CleanName(v.GetValueOrDefault("veli_adi"));
        var first = TextRules.CleanName(v.GetValueOrDefault("ogrenci_adi"));
        var last = TextRules.CleanName(v.GetValueOrDefault("ogrenci_soyadi"));
        if (parent.Length == 0) errors.Add("Veli adı boş.");
        if (first.Length == 0) errors.Add("Öğrenci adı boş.");
        var phone = TextRules.NormalizePhone(v.GetValueOrDefault("veli_telefon"));
        if (phone is not null && phone.Length != 10) warnings.Add("Telefon 10 haneli değil; olduğu gibi alınacak.");
        var birth = ParseYear(v.GetValueOrDefault("dogum_yili"), errors);
        var balance = ParseMoney(v.GetValueOrDefault("acilis_bakiyesi"), "Açılış bakiyesi", errors);

        var parentKey = phone ?? TextRules.SearchKey(parent);
        // Telefon eslesirse guvenli sekilde mevcut cariye baglanir. Telefon yoksa/eslesmezse SADECE
        // isme bakarak birlestirme yapilmaz (X:DATA-01): iki farkli veli ayni adi tasiyabilir. Bu
        // durumda yeni cari acilir, ama kullaniciyi uyarmak icin olasi eslesme bilgi olarak gosterilir.
        var matchedByPhone = phone is not null ? ctx.PartiesByPhone.GetValueOrDefault(phone) : null;
        var possibleNameMatch = matchedByPhone is null ? ctx.PartiesByName.GetValueOrDefault(TextRules.SearchKey(parent)) : null;
        if (matchedByPhone is not null) warnings.Add($"Mevcut cariye bağlanacak: {matchedByPhone.Code} {matchedByPhone.Name}.");
        else if (possibleNameMatch is not null)
            warnings.Add($"DİKKAT: '{possibleNameMatch.Code} {possibleNameMatch.Name}' adında bir cari zaten var ama telefon eşleşmedi; " +
                "otomatik birleştirilmeyecek, yeni ve ayrı bir cari olarak eklenecek. Aynı kişiyse aktarımdan sonra elle birleştirin.");
        if (!seen.Add($"{parentKey}|{TextRules.SearchKey(first, last)}"))
            warnings.Add("Aynı veli ve öğrenci dosyada tekrar ediyor; ikinci kez oluşturulmayacak.");
        if (balance is not null) warnings.Add("Açılış bakiyesi cariye doğrudan yazılmaz; muhasebe onayına düşer.");

        return (new Dictionary<string, string?>
        {
            ["veli_adi"] = parent, ["veli_telefon"] = phone, ["ogrenci_adi"] = first, ["ogrenci_soyadi"] = last,
            ["grup"] = TextRules.CleanName(v.GetValueOrDefault("grup")) is { Length: > 0 } g ? g : null,
            ["dogum_yili"] = birth?.ToString(CultureInfo.InvariantCulture),
            ["acilis_bakiyesi"] = balance?.ToString(CultureInfo.InvariantCulture),
        }, errors, warnings);
    }

    private static (Dictionary<string, string?>, List<string>, List<string>) ValidateParty(
        Dictionary<string, string?> v, MatchContext ctx, HashSet<string> seen)
    {
        var errors = new List<string>();
        var warnings = new List<string>();
        var name = TextRules.CleanName(v.GetValueOrDefault("cari_adi"));
        if (name.Length == 0) errors.Add("Cari adı boş.");
        var code = v.GetValueOrDefault("cari_kodu")?.Trim().ToUpperInvariant();
        if (!string.IsNullOrEmpty(code)) code = Companies.CompanyCodes.WithPrefix(ctx.CompanyCode, code);
        var kind = ParsePartyKind(v.GetValueOrDefault("tur"), errors);
        var phone = TextRules.NormalizePhone(v.GetValueOrDefault("telefon"));
        var email = TextRules.NormalizeEmail(v.GetValueOrDefault("eposta"));
        if (email is not null && !email.Contains('@', StringComparison.Ordinal)) errors.Add("E-posta geçersiz.");
        var balance = ParseMoney(v.GetValueOrDefault("acilis_bakiyesi"), "Açılış bakiyesi", errors);
        if (!string.IsNullOrEmpty(code) && ctx.PartiesByCode.TryGetValue(code, out var byCode))
            warnings.Add($"Bu kodla cari zaten var ({byCode.Name}); yeni cari oluşturulmayacak.");
        var dupKey = string.IsNullOrEmpty(code) ? TextRules.SearchKey(name) : code;
        if (!seen.Add(dupKey)) errors.Add("Aynı cari dosyada tekrar ediyor.");
        if (balance is not null) warnings.Add("Açılış bakiyesi cariye doğrudan yazılmaz; muhasebe onayına düşer.");
        return (new Dictionary<string, string?>
        {
            ["cari_adi"] = name, ["cari_kodu"] = string.IsNullOrEmpty(code) ? null : code, ["tur"] = kind.ToString(),
            ["telefon"] = phone, ["eposta"] = email,
            ["vergi_no"] = v.GetValueOrDefault("vergi_no") is { } t ? new string(t.Where(char.IsAsciiDigit).ToArray()) : null,
            ["acilis_bakiyesi"] = balance?.ToString(CultureInfo.InvariantCulture),
        }, errors, warnings);
    }

    private static (Dictionary<string, string?>, List<string>, List<string>) ValidateItem(
        Dictionary<string, string?> v, MatchContext ctx, HashSet<string> seen)
    {
        var errors = new List<string>();
        var warnings = new List<string>();
        var name = TextRules.CleanName(v.GetValueOrDefault("ad"));
        if (name.Length == 0) errors.Add("Ad boş.");
        var code = v.GetValueOrDefault("kod")?.Trim().ToUpperInvariant();
        if (!string.IsNullOrEmpty(code)) code = Companies.CompanyCodes.WithPrefix(ctx.CompanyCode, code);
        if (!string.IsNullOrEmpty(code) && ctx.ItemCodes.Contains(code)) warnings.Add("Bu kodla kalem zaten var; atlanacak.");
        var kindText = TextRules.SearchKey(v.GetValueOrDefault("tur"));
        var kind = kindText.StartsWith("stok", StringComparison.Ordinal) ? ItemKind.Stock : ItemKind.Service;
        var price = ParseMoney(v.GetValueOrDefault("birim_fiyat"), "Birim fiyat", errors, allowFourDecimals: true);
        if (price is null && errors.Count == 0) errors.Add("Birim fiyat boş.");
        if (price < 0) errors.Add("Birim fiyat eksi olamaz.");
        decimal? rate = null;
        if (TextRules.TryParseDecimal(v.GetValueOrDefault("kdv_orani")?.Replace("%", "", StringComparison.Ordinal), out var r))
            rate = r is > 0 and < 1 ? r * 100 : r;
        if (rate is null or < 0 or > 100) errors.Add("KDV oranı geçersiz.");
        var inclText = TextRules.SearchKey(v.GetValueOrDefault("kdv_dahil"));
        var incl = inclText.Length == 0 || inclText is "evet" or "e" or "1" or "true" or "dahil";
        if (!seen.Add(string.IsNullOrEmpty(code) ? TextRules.SearchKey(name) : code)) errors.Add("Aynı kalem dosyada tekrar ediyor.");
        return (new Dictionary<string, string?>
        {
            ["ad"] = name, ["kod"] = string.IsNullOrEmpty(code) ? null : code, ["tur"] = kind.ToString(),
            ["birim"] = string.IsNullOrWhiteSpace(v.GetValueOrDefault("birim")) ? "adet" : v["birim"]!.Trim(),
            ["birim_fiyat"] = price?.ToString(CultureInfo.InvariantCulture),
            ["kdv_orani"] = rate?.ToString(CultureInfo.InvariantCulture), ["kdv_dahil"] = incl ? "1" : "0",
        }, errors, warnings);
    }

    // ------------------------------------------------------------------ uygulama

    private async Task ApplyStudents(ImportBatch batch, List<ImportRow> rows, CancellationToken ct)
    {
        var ctx = await BuildMatchContext(ImportKinds.Parties, ct);
        var nextCode = await NextNumber(ct);
        var created = new Dictionary<string, Party>(StringComparer.Ordinal);
        var athleteKeys = (await db.Athletes.Where(a => a.IsActive).Select(a => new { a.PartyId, a.SearchKey }).ToListAsync(ct))
            .Select(a => $"{a.PartyId}|{a.SearchKey}").ToHashSet(StringComparer.Ordinal);
        var balances = new Dictionary<Guid, decimal>();

        foreach (var row in rows)
        {
            var v = Data(row);
            var phone = v["veli_telefon"];
            var parentName = v["veli_adi"]!;
            var key = phone ?? TextRules.SearchKey(parentName);
            // Telefon yoksa isme bakarak mevcut cariye baglanmaz (bkz. ValidateStudent); yalniz
            // ayni dosya icindeki tekrarlar (created) ve telefon eslesmesi guvenli sayilir.
            var party = created.GetValueOrDefault(key)
                        ?? (phone is not null ? ctx.PartiesByPhone.GetValueOrDefault(phone) : null);
            if (party is null)
            {
                party = NewParty(batch, $"{ctx.CompanyCode}-C{nextCode++:000000}", parentName, PartyKind.Customer, phone, null, null);
                created[key] = party;
            }
            row.PartyId = party.Id;

            var athleteKey = $"{party.Id}|{TextRules.SearchKey(v["ogrenci_adi"], v["ogrenci_soyadi"])}";
            if (athleteKeys.Add(athleteKey))
            {
                var athlete = new Athlete
                {
                    TenantId = batch.TenantId, CreatedByUserId = user.UserId, PartyId = party.Id, ImportBatchId = batch.Id,
                    FirstName = v["ogrenci_adi"]!, LastName = v["ogrenci_soyadi"] ?? string.Empty, GroupName = v["grup"],
                    BirthYear = v["dogum_yili"] is { } y ? int.Parse(y, CultureInfo.InvariantCulture) : null,
                    SearchKey = TextRules.SearchKey(v["ogrenci_adi"], v["ogrenci_soyadi"]),
                };
                db.Athletes.Add(athlete);
                row.AthleteId = athlete.Id;
            }
            if (v["acilis_bakiyesi"] is { } b)
                balances[party.Id] = balances.GetValueOrDefault(party.Id) + decimal.Parse(b, CultureInfo.InvariantCulture);
        }
        await AddOpeningBalances(batch, rows, balances, ct);
    }

    private async Task ApplyParties(ImportBatch batch, List<ImportRow> rows, CancellationToken ct)
    {
        var ctx = await BuildMatchContext(ImportKinds.Parties, ct);
        var nextCode = await NextNumber(ct);
        var balances = new Dictionary<Guid, decimal>();
        foreach (var row in rows)
        {
            var v = Data(row);
            var code = v["cari_kodu"];
            var party = code is not null ? ctx.PartiesByCode.GetValueOrDefault(code) : null;
            if (party is null)
            {
                party = NewParty(batch, code ?? $"{ctx.CompanyCode}-C{nextCode++:000000}", v["cari_adi"]!, Enum.Parse<PartyKind>(v["tur"]!),
                    v["telefon"], v["eposta"], v["vergi_no"]);
                ctx.PartiesByCode[party.Code] = party;
            }
            row.PartyId = party.Id;
            if (v["acilis_bakiyesi"] is { } b)
                balances[party.Id] = balances.GetValueOrDefault(party.Id) + decimal.Parse(b, CultureInfo.InvariantCulture);
        }
        await AddOpeningBalances(batch, rows, balances, ct);
    }

    private async Task ApplyItems(ImportBatch batch, List<ImportRow> rows, CancellationToken ct)
    {
        var codes = (await db.Items.Select(i => i.Code).ToListAsync(ct)).ToHashSet(StringComparer.Ordinal);
        var itemPrefix = await tenant.RequireCodeAsync(db, ct) + "-U";
        var next = (await db.Items.Where(i => i.Code.StartsWith(itemPrefix)).Select(i => i.Code).ToListAsync(ct))
            .Select(c => int.TryParse(c.AsSpan(itemPrefix.Length), out var n) ? n : 0).DefaultIfEmpty(0).Max() + 1;
        foreach (var row in rows)
        {
            var v = Data(row);
            var code = v["kod"] ?? $"{itemPrefix}{next++:000000}";
            if (!codes.Add(code)) continue;
            var item = new Item { TenantId = batch.TenantId, CreatedByUserId = user.UserId, ImportBatchId = batch.Id, Code = code };
            CatalogEndpoints.ApplyItem(item, new ItemInput(v["ad"]!, Enum.Parse<ItemKind>(v["tur"]!), code, v["birim"],
                decimal.Parse(v["birim_fiyat"]!, CultureInfo.InvariantCulture), v["kdv_dahil"] == "1",
                decimal.Parse(v["kdv_orani"]!, CultureInfo.InvariantCulture)));
            db.Items.Add(item);
            row.ItemId = item.Id;
        }
    }

    private async Task AddOpeningBalances(ImportBatch batch, List<ImportRow> rows, Dictionary<Guid, decimal> balances, CancellationToken ct)
    {
        var partyIds = balances.Keys.ToList();
        var existing = (await db.OpeningBalances.Where(o => partyIds.Contains(o.PartyId) && o.Status != OpeningBalanceStatus.Rejected)
            .Select(o => o.PartyId).ToListAsync(ct)).ToHashSet();
        foreach (var (partyId, amount) in balances)
        {
            if (amount == 0) continue;
            if (existing.Contains(partyId))
            {
                foreach (var r in rows.Where(r => r.PartyId == partyId))
                    r.Warnings = (r.Warnings + " Carinin zaten açılış bakiyesi var; yenisi oluşturulmadı.").Trim();
                continue;
            }
            var ob = new OpeningBalance
            {
                TenantId = batch.TenantId, CreatedByUserId = user.UserId, PartyId = partyId, Amount = amount,
                ImportBatchId = batch.Id, Note = $"Excel aktarımı: {batch.FileName}",
            };
            db.OpeningBalances.Add(ob);
            foreach (var r in rows.Where(r => r.PartyId == partyId)) r.OpeningBalanceId = ob.Id;
        }
    }

    private Party NewParty(ImportBatch batch, string code, string name, PartyKind kind, string? phone, string? email, string? taxNo)
    {
        var p = new Party { TenantId = batch.TenantId, CreatedByUserId = user.UserId, ImportBatchId = batch.Id, Code = code };
        PartyService.Apply(p, new PartyInput(name, kind, code, phone, email, taxNo, null));
        db.Parties.Add(p);
        return p;
    }

    private async Task<int> NextNumber(CancellationToken ct)
    {
        var prefix = await tenant.RequireCodeAsync(db, ct) + "-C";
        var next = await PartyService.NextCodeAsync(db, prefix, ct);
        return int.Parse(next.AsSpan(prefix.Length), CultureInfo.InvariantCulture);
    }

    // ------------------------------------------------------------------ sablon indirme (surum 2 hazirlik notu madde 1)

    /// <summary>
    /// Zorunlu alanlar kalin ve isaretli, ikinci satirda sentetik ornek deger bulunan bos
    /// sablon uretir. Serbest sutun eslestirmeyi degistirmez; sablon basliklari zaten
    /// ImportKinds.Fields ile birebir aynidir, bu yuzden geri yuklendiginde otomatik eslesir.
    /// Ogrenci sablonunda sayfa adi kasitli olarak "Sayfa1 (2)" ile ayni verilir.
    /// </summary>
    public static byte[] BuildTemplate(string kind)
    {
        ImportKinds.Validate(kind);
        var fields = ImportKinds.Fields[kind];
        using var wb = new XLWorkbook();
        var sheetName = kind == ImportKinds.Students ? ImportKinds.StudentSheet : ImportKinds.KindTitle[kind];
        var ws = wb.AddWorksheet(sheetName);

        for (var c = 0; c < fields.Length; c++)
        {
            var f = fields[c];
            var header = ws.Cell(1, c + 1);
            header.Value = f.Required ? $"{f.Label} *" : f.Label;
            header.Style.Font.Bold = true;
            if (f.Required)
            {
                header.Style.Fill.BackgroundColor = XLColor.FromArgb(255, 243, 224);
                header.Style.Font.FontColor = XLColor.FromArgb(122, 75, 0);
            }
            var example = ws.Cell(2, c + 1);
            example.Value = ImportKinds.ExampleValues.GetValueOrDefault(f.Key, "");
            example.Style.Font.FontColor = XLColor.Gray;
            example.Style.Font.Italic = true;
        }
        ws.Row(1).Height = 20;
        ws.SheetView.FreezeRows(1);
        ws.Columns(1, fields.Length).AdjustToContents(1, 2);

        if (kind == ImportKinds.Students)
        {
            // Gercek dosyalarla ayni yapi olsun diye baska sayfa isimleri de yer alir;
            // aktarim yalniz "Sayfa1 (2)" sayfasini okur, digerleri bilgi amaclidir.
            var decoy = wb.AddWorksheet("Sayfa1");
            decoy.Cell(1, 1).Value = "Bu sayfa okunmaz. Veri yalnız \"Sayfa1 (2)\" sayfasından alınır.";
        }

        using var ms = new MemoryStream();
        wb.SaveAs(ms);
        return ms.ToArray();
    }

    // ------------------------------------------------------------------ yardimcilar

    private static Dictionary<string, string?> Data(ImportRow row) =>
        JsonSerializer.Deserialize<Dictionary<string, string?>>(row.DataJson) ?? [];

    private static XLWorkbook Open(byte[] file)
    {
        try
        {
            return new XLWorkbook(new MemoryStream(file, writable: false));
        }
        catch (Exception)
        {
            throw new DomainException("file", "Dosya okunamadı. Yalnız .xlsx biçimindeki Excel dosyaları desteklenir.");
        }
    }

    private static string? ResolveSheetName(List<string> sheets, string kind, string? requested)
    {
        if (kind == ImportKinds.Students)
        {
            if (!sheets.Contains(ImportKinds.StudentSheet, StringComparer.Ordinal))
                throw new DomainException("sheet",
                    $"Dosyada '{ImportKinds.StudentSheet}' sayfası yok. Öğrenci aktarımı yalnız bu sayfadan yapılır; benzer adlı sayfalar kullanılmaz.");
            if (requested is not null && requested != ImportKinds.StudentSheet)
                throw new DomainException("sheet", $"Öğrenci aktarımı yalnız '{ImportKinds.StudentSheet}' sayfasından yapılır.");
            return ImportKinds.StudentSheet;
        }
        if (requested is null) return sheets.FirstOrDefault();
        return sheets.Contains(requested, StringComparer.Ordinal) ? requested : throw new DomainException("sheet", "Sayfa bulunamadı.");
    }

    private static SheetInfo Describe(IXLWorksheet ws)
    {
        var used = ws.RangeUsed();
        if (used is null) return new SheetInfo(ws.Name, [], 0);
        var header = used.FirstRow();
        var columns = header.Cells().Select(c => new ColumnInfo(c.Address.ColumnNumber, c.Address.ColumnLetter,
            string.IsNullOrWhiteSpace(CellText(c)) ? $"Sütun {c.Address.ColumnLetter}" : CellText(c)!)).ToList();
        return new SheetInfo(ws.Name, columns, used.RowCount() - 1);
    }

    /// <summary>Hucre bicimi degil ham deger okunur (ENS tuzagi: Turkce yerelde "0.##" -> "500,").</summary>
    private static string? CellText(IXLCell cell)
    {
        var value = cell.Value;
        if (value.IsBlank) return null;
        if (value.IsNumber) return ((decimal)value.GetNumber()).ToString("0.##########", CultureInfo.InvariantCulture);
        if (value.IsDateTime) return value.GetDateTime().ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        if (value.IsBoolean) return value.GetBoolean() ? "1" : "0";
        if (value.IsText) return value.GetText().Trim();
        return null;
    }

    private static decimal? ParseMoney(string? text, string label, List<string> errors, bool allowFourDecimals = false)
    {
        if (string.IsNullOrWhiteSpace(text)) return null;
        if (!TextRules.TryParseDecimal(text, out var value)) { errors.Add($"{label} sayı değil: '{text}'."); return null; }
        var ok = allowFourDecimals ? Money.RoundQuantity(value) == value : Money.HasAtMostTwoDecimals(value);
        if (!ok) { errors.Add($"{label} çok fazla ondalık basamak içeriyor."); return null; }
        return value == 0 ? null : value;
    }

    private static int? ParseYear(string? text, List<string> errors)
    {
        if (string.IsNullOrWhiteSpace(text)) return null;
        if (int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var y) && y is >= 1990 and <= 2030) return y;
        if (DateOnly.TryParseExact(text, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var d) && d.Year is >= 1990 and <= 2030)
            return d.Year;
        errors.Add($"Doğum yılı geçersiz: '{text}'.");
        return null;
    }

    private static PartyKind ParsePartyKind(string? text, List<string> errors)
    {
        var key = TextRules.SearchKey(text);
        if (key.Length == 0 || key.StartsWith("mus", StringComparison.Ordinal) || key.StartsWith("veli", StringComparison.Ordinal)) return PartyKind.Customer;
        if (key.StartsWith("ted", StringComparison.Ordinal)) return PartyKind.Supplier;
        if (key.StartsWith("iki", StringComparison.Ordinal) || key.StartsWith("her", StringComparison.Ordinal)) return PartyKind.Both;
        errors.Add($"Cari türü tanınmadı: '{text}'.");
        return PartyKind.Customer;
    }
}
