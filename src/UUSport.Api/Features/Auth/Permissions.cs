using UUSport.Api.Domain;

namespace UUSport.Api.Features.Auth;

/// <summary>
/// Yetki anahtarlari (ADR-012 2.4): tek liste; API politikasi (`perm:&lt;anahtar&gt;`), ara katmandaki yazma
/// engeli ve arayuz menusu ayni listeden beslenir (guvenlik standardi bolum 3 "tek matris"). Ekran basina
/// tik kutusu matrisi yapilmaz; rol sablonu + uyelik basina ver/kaldir ile ince ayar yapilir.
/// </summary>
public static class Permissions
{
    /// <summary>Herhangi bir yazma islemi (POST/PUT/DELETE). Ortak icin kapali ve ACILAMAZ.</summary>
    public const string Write = "write";
    public const string PartiesRead = "parties.read";
    public const string LedgerRead = "ledger.read";
    public const string DocumentsRead = "documents.read";
    public const string CatalogRead = "catalog.read";
    public const string CashRead = "cash.read";
    public const string ReportsSummary = "reports.summary";
    public const string ReportsDetail = "reports.detail";
    public const string UsersManage = "users.manage";
    public const string AuditRead = "audit.read";

    public static readonly string[] All =
    [
        Write, PartiesRead, LedgerRead, DocumentsRead, CatalogRead, CashRead, ReportsSummary, ReportsDetail,
        UsersManage, AuditRead,
    ];

    /// <summary>Uyelik basina verilebilen anahtarlar: yazma ve kullanici yonetimi sablon disina verilemez.</summary>
    public static readonly string[] Grantable =
        [PartiesRead, LedgerRead, DocumentsRead, CatalogRead, CashRead, ReportsSummary, ReportsDetail, AuditRead];

    public static string PolicyName(string key) => "perm:" + key;

    public static IReadOnlySet<string> Template(string role) => role switch
    {
        Roles.Yonetici => new HashSet<string>(All, StringComparer.Ordinal),
        Roles.Muhasebe => new HashSet<string>(
            [Write, PartiesRead, LedgerRead, DocumentsRead, CatalogRead, CashRead, ReportsSummary, ReportsDetail],
            StringComparer.Ordinal),
        // Ortak: varsayilan ozet raporlar; ayrinti yonetici acar (KU-10). Yazma yok.
        Roles.Ortak => new HashSet<string>([ReportsSummary], StringComparer.Ordinal),
        _ => new HashSet<string>(StringComparer.Ordinal),
    };

    /// <summary>Sablon + verilen - alinan. Verilebilir olmayan anahtar (write, users.manage) eklenmez.</summary>
    public static IReadOnlySet<string> Effective(string role, IEnumerable<string> granted, IEnumerable<string> denied)
    {
        var set = new HashSet<string>(Template(role), StringComparer.Ordinal);
        foreach (var g in granted) if (Grantable.Contains(g)) set.Add(g);
        foreach (var d in denied) if (d != Write && d != UsersManage) set.Remove(d);
        return set;
    }

    public static void ValidateKeys(IEnumerable<string> keys, string field)
    {
        foreach (var k in keys)
            if (!Grantable.Contains(k))
                throw new Infrastructure.DomainException(field, $"Geçersiz yetki anahtarı: {k}");
    }
}
