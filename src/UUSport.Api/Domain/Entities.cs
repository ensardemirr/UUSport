using Microsoft.AspNetCore.Identity;

namespace UUSport.Api.Domain;

public abstract class TenantEntity
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public Guid TenantId { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public Guid? CreatedByUserId { get; set; }
}

/// <summary>
/// Sirket (tuzel kisilik, bir vergi kimlik numarasi) = kiraci (ADR-012 2.1). Her sirketin kendi
/// carileri, katalogu, kasa/banka hesaplari ve belge numaralari vardir; RLS siniri budur.
/// </summary>
public sealed class Tenant
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public string Name { get; set; } = string.Empty;
    /// <summary>Kisa kod (ASCII buyuk harf, 2-6): ekranda rozet ve cari/urun kodlarinda on ek (`UUS-C000001`).</summary>
    public string Code { get; set; } = string.Empty;
    public string? LegalName { get; set; }
    /// <summary>Vergi kimlik numarasi; yalniz canlida girilir, gelistirmede bos kalir.</summary>
    public string? TaxNumber { get; set; }
    public string? TaxOffice { get; set; }
    public bool IsActive { get; set; } = true;
    public short SortOrder { get; set; }
    /// <summary>Arayuzdeki sirket rengi anahtari (erisilebilir paletten).</summary>
    public string? ColorKey { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}

/// <summary>Sirket basina roller (ADR-012 2.2). Identity rol tablosu artik yetki kaynagi degildir.</summary>
public static class Roles
{
    public const string Yonetici = "Yonetici";
    public const string Muhasebe = "Muhasebe";
    /// <summary>Salt okunur ortak: ne gorecegi yetki anahtarlariyla parametriktir, yazamaz (ADR-012 2.4).</summary>
    public const string Ortak = "Ortak";
    public static readonly string[] All = [Yonetici, Muhasebe, Ortak];
}

public sealed class AppUser : IdentityUser<Guid>
{
    /// <summary>Ev sirketi: baslik gonderilmediginde varsayilan secim. Uyelik tablosu asil kaynaktir.</summary>
    public Guid TenantId { get; set; }
    public string FullName { get; set; } = string.Empty;
    /// <summary>Kullanicinin uygulayabilecegi en yuksek iskonto yuzdesi (akis belgesi 3.1).</summary>
    public decimal MaxDiscountPercent { get; set; }
    public bool IsActive { get; set; } = true;
    /// <summary>Grup yoneticisi (patron): tum sirketleri gorur; sirket, kullanici ve uyelik yonetir (ADR-012 2.2).</summary>
    public bool IsGroupAdmin { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}

/// <summary>
/// Kullanicinin bir sirketteki rolu (ADR-012 2.2). RLS'e tabi degildir (grup duzeyinde kullanici
/// yonetimi); erisimi API katmaninda grup yoneticisi veya o sirketin Yonetici'si alir.
/// </summary>
public sealed class CompanyMembership
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public Guid UserId { get; set; }
    public Guid TenantId { get; set; }
    public string Role { get; set; } = Roles.Muhasebe;
    public bool IsActive { get; set; } = true;
    /// <summary>Rol sablonuna ek verilen yetki anahtarlari (Ortak icin parametrik gorunurluk, ADR-012 2.4).</summary>
    public string[] GrantedKeys { get; set; } = [];
    /// <summary>Rol sablonundan alinan yetki anahtarlari.</summary>
    public string[] DeniedKeys { get; set; } = [];
    public Guid? GrantedByUserId { get; set; }
    public DateTimeOffset GrantedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? RevokedAt { get; set; }
}

public sealed class AppRole : IdentityRole<Guid>
{
    public AppRole() { }
    public AppRole(string name) : base(name) { }
}

// ---------- Cari ve sporcu ----------

public enum PartyKind { Customer, Supplier, Both }

/// <summary>Cari hesap (veli, musteri veya tedarikci). Ayni kisi icin tek cari (ADR-004).</summary>
public sealed class Party : TenantEntity
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public PartyKind Kind { get; set; } = PartyKind.Customer;
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? TaxNumber { get; set; }
    public string? Notes { get; set; }
    /// <summary>Mali musavirin mizanindaki hesap kodu. Bos olabilir; dolunca GMS.NET aktariminda kullanilir (surum 2 hazirlik notu madde 2).</summary>
    public string? AccountCode { get; set; }
    public string SearchKey { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public Guid? ImportBatchId { get; set; }
    /// <summary>Grup ici cari: bu cari grubun baska bir sirketini temsil eder (ADR-012 2.5).</summary>
    public Guid? RelatedTenantId { get; set; }
}

public sealed class Athlete : TenantEntity
{
    public Guid PartyId { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? GroupName { get; set; }
    public int? BirthYear { get; set; }
    public string SearchKey { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public Guid? ImportBatchId { get; set; }
}

// ---------- Katalog ve stok ----------

public enum ItemKind { Stock, Service }

public sealed class Item : TenantEntity
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public ItemKind Kind { get; set; }
    public string Unit { get; set; } = "adet";
    public decimal UnitPrice { get; set; }
    public bool PriceIncludesVat { get; set; } = true;
    public decimal VatRate { get; set; }
    /// <summary>Mali musavirin mizanindaki hesap kodu. Bos olabilir (surum 2 hazirlik notu madde 2).</summary>
    public string? AccountCode { get; set; }
    public string SearchKey { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public Guid? ImportBatchId { get; set; }
}

/// <summary>
/// Tedarikcinin ayni urun icin kullandigi kod/barkod (ADR-012 2.8). Bizim kodumuz ile tedarikci kodu
/// ayri kavramdir; alis faturasi girisinde satir once buradan aranir, boylece ayni urun ikinci kez acilmaz.
/// </summary>
public sealed class SupplierItemCode : TenantEntity
{
    public Guid ItemId { get; set; }
    public Guid SupplierPartyId { get; set; }
    public string SupplierCode { get; set; } = string.Empty;
    public string? Barcode { get; set; }
}

/// <summary>Stok hareketi yalniz satis/alis satirindan veya tersinden uretilir (ADR-004 2.2).</summary>
public sealed class StockMovement : TenantEntity
{
    public Guid ItemId { get; set; }
    public decimal Quantity { get; set; }
    public string SourceType { get; set; } = string.Empty;
    public Guid SourceId { get; set; }
    public DateOnly OccurredOn { get; set; }
}

// ---------- Satis / alis satiri ----------

public enum TradeDirection { Sale, Purchase }

/// <summary>
/// Satis veya alis satiri: ekonomik olayin kendisi. Cariye tutar yazan iki kaynaktan biri
/// (ADR-004). Iade/duzeltme, ReversesLineId dolu ve miktari eksi bir satirla yapilir.
/// </summary>
public sealed class TradeLine : TenantEntity
{
    public Guid PartyId { get; set; }
    public Guid ItemId { get; set; }
    public TradeDirection Direction { get; set; }
    public DateOnly OccurredOn { get; set; }
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public bool PriceIncludesVat { get; set; }
    public decimal DiscountPercent { get; set; }
    public decimal VatRate { get; set; }
    /// <summary>Matrah; numeric(18,6).</summary>
    public decimal NetAmount { get; set; }
    /// <summary>Satir KDV'si; satirda yuvarlanmaz, numeric(18,6) (ADR-002).</summary>
    public decimal VatAmount { get; set; }
    /// <summary>Cariye yazilan brut tutar; numeric(18,2).</summary>
    public decimal GrossAmount { get; set; }
    public decimal DocumentedQuantity { get; set; }
    public Guid? ReversesLineId { get; set; }
    public string? Description { get; set; }
}

// ---------- Belge (fatura) ----------

public enum DocumentType { EArsiv, EFatura, Kagit, Fis }
public enum DocumentStatus { Issued, Cancelled }

/// <summary>
/// Resmi belge. Cariye ve stoka tutar YAZMAZ; yalniz satirlari belgeler (ADR-004).
/// Ilk surumde GIB portalinda kesilip elle girilir; tutarlar yazdigi gibi alinir (ADR-005).
/// </summary>
public sealed class Document : TenantEntity
{
    public Guid PartyId { get; set; }
    public TradeDirection Direction { get; set; }
    public DocumentType Type { get; set; }
    public string DocumentNo { get; set; } = string.Empty;
    public DateOnly DocumentDate { get; set; }
    public decimal NetTotal { get; set; }
    public decimal VatTotal { get; set; }
    public decimal GrossTotal { get; set; }
    public DocumentStatus Status { get; set; } = DocumentStatus.Issued;
    public string? Note { get; set; }
    public string? CancelReason { get; set; }
    public DateTimeOffset? CancelledAt { get; set; }
    public Guid? CancelledByUserId { get; set; }
    public List<DocumentVatLine> VatLines { get; set; } = [];
    public List<DocumentLink> Links { get; set; } = [];
}

public sealed class DocumentVatLine : TenantEntity
{
    public Guid DocumentId { get; set; }
    public decimal VatRate { get; set; }
    public decimal NetAmount { get; set; }
    public decimal VatAmount { get; set; }
}

public sealed class DocumentLink : TenantEntity
{
    public Guid DocumentId { get; set; }
    public Guid TradeLineId { get; set; }
    public decimal Quantity { get; set; }
    public bool IsActive { get; set; } = true;
}

// ---------- Kasa / banka ve tahsilat / odeme ----------

/// <summary>
/// CreditCard ve Loan gercek tahsilat/odeme hesabi degildir; yalniz raporlama ve
/// gruplama icin bir banka hesabinin altina baglanir (surum 2 hazirlik notu madde 3).
/// Odeme kaydi (Payment) her zaman Cash/Bank/Pos turunde bir hesaba yazilir.
/// </summary>
public enum CashAccountKind { Cash, Bank, Pos, CreditCard, Loan }

public sealed class CashAccount : TenantEntity
{
    public string Name { get; set; } = string.Empty;
    public CashAccountKind Kind { get; set; }
    /// <summary>Yalniz CreditCard/Loan icin dolu; bir Bank hesabini gosterir (kural: uygulama katmaninda).</summary>
    public Guid? ParentAccountId { get; set; }
    public bool IsActive { get; set; } = true;
}

public enum PaymentDirection { Receipt, Disbursement }
public enum PaymentMethod { Cash, Transfer, Card, Check }

/// <summary>Tahsilat (Receipt) veya odeme (Disbursement). Cariye tutar yazan ikinci kaynak.</summary>
public sealed class Payment : TenantEntity
{
    public Guid PartyId { get; set; }
    public PaymentDirection Direction { get; set; }
    public PaymentMethod Method { get; set; }
    public Guid CashAccountId { get; set; }
    public decimal Amount { get; set; }
    public DateOnly OccurredOn { get; set; }
    public string? Reference { get; set; }
    public string? Description { get; set; }
    public Guid? ReversesPaymentId { get; set; }
}

/// <summary>Tahsilat dagitimi. Cariye tutar YAZMAZ. Geri alma eksi tutarli satirla yapilir.</summary>
public sealed class PaymentAllocation : TenantEntity
{
    public Guid PaymentId { get; set; }
    public Guid TradeLineId { get; set; }
    public decimal Amount { get; set; }
    public Guid? ReversesAllocationId { get; set; }
}

// ---------- Acilis bakiyesi ----------

public enum OpeningBalanceStatus { Pending, Approved, Rejected }

/// <summary>Acilis bakiyesi muhasebe onayi olmadan cariye yazilmaz (ana hafiza 3.1).</summary>
public sealed class OpeningBalance : TenantEntity
{
    public Guid PartyId { get; set; }
    /// <summary>Arti: cari bize borclu. Eksi: cari alacakli.</summary>
    public decimal Amount { get; set; }
    public OpeningBalanceStatus Status { get; set; } = OpeningBalanceStatus.Pending;
    public string? Note { get; set; }
    public Guid? ImportBatchId { get; set; }
    public Guid? DecidedByUserId { get; set; }
    public DateTimeOffset? DecidedAt { get; set; }
}

// ---------- Cari hareket ----------

public static class LedgerSources
{
    public const string SalesLine = "sales_line";
    public const string SalesLineReversal = "sales_line_reversal";
    public const string PurchaseLine = "purchase_line";
    public const string PurchaseLineReversal = "purchase_line_reversal";
    public const string Payment = "payment";
    public const string PaymentReversal = "payment_reversal";
    public const string OpeningBalance = "opening_balance";
    public const string ApprovedAdjustment = "approved_adjustment";

    public static readonly string[] All =
        [SalesLine, SalesLineReversal, PurchaseLine, PurchaseLineReversal, Payment, PaymentReversal, OpeningBalance, ApprovedAdjustment];
}

/// <summary>
/// Cariyi etkileyen TEK tablo (ADR-004 2.2). Kaynak basina en fazla bir satir; belge kaynak olamaz.
/// Borc = cari bize borclanir. Bakiye = borc - alacak.
/// </summary>
public sealed class LedgerEntry : TenantEntity
{
    public Guid PartyId { get; set; }
    public DateOnly OccurredOn { get; set; }
    public string SourceType { get; set; } = string.Empty;
    public Guid SourceId { get; set; }
    public decimal Debit { get; set; }
    public decimal Credit { get; set; }
    public string Description { get; set; } = string.Empty;
}

// ---------- Excel aktarimi ----------

public enum ImportStatus { Previewed, Applied, Reverted }

public sealed class ImportBatch : TenantEntity
{
    public string Kind { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public string FileSha256 { get; set; } = string.Empty;
    public string SheetName { get; set; } = string.Empty;
    public string MappingJson { get; set; } = "{}";
    public ImportStatus Status { get; set; } = ImportStatus.Previewed;
    public int RowsRead { get; set; }
    public int RowsValid { get; set; }
    public int RowsError { get; set; }
    public DateTimeOffset? AppliedAt { get; set; }
    public Guid? AppliedByUserId { get; set; }
    public DateTimeOffset? RevertedAt { get; set; }
    public Guid? RevertedByUserId { get; set; }
}

public sealed class ImportRow : TenantEntity
{
    public Guid BatchId { get; set; }
    public int RowNumber { get; set; }
    public string DataJson { get; set; } = "{}";
    public bool IsValid { get; set; }
    public string? Errors { get; set; }
    public string? Warnings { get; set; }
    public Guid? PartyId { get; set; }
    public Guid? AthleteId { get; set; }
    public Guid? ItemId { get; set; }
    public Guid? OpeningBalanceId { get; set; }
}

// ---------- Kredi ve taksit tablosu (surum 2 hazirlik notu madde 3) ----------

/// <summary>
/// Kredi. Borc veren banka/kurum bir Party'dir (kind=Supplier); taksit odemesi
/// normal bir Payment(Disbursement) olarak o Party'ye yazilir (ADR-004 modeli
/// degismez). CashAccountId yalniz raporlama icindir (Kind=Loan, bankanin altinda).
/// </summary>
public sealed class Loan : TenantEntity
{
    public Guid PartyId { get; set; }
    public Guid CashAccountId { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal PrincipalAmount { get; set; }
    public DateOnly StartDate { get; set; }
    public string? Notes { get; set; }
    public bool IsActive { get; set; } = true;
}

public sealed class LoanInstallment : TenantEntity
{
    public Guid LoanId { get; set; }
    public int InstallmentNo { get; set; }
    public DateOnly DueDate { get; set; }
    public decimal PrincipalAmount { get; set; }
    public decimal InterestAmount { get; set; }
    public decimal FeeAmount { get; set; }
    /// <summary>Odendiginde dolar; ilgili Payment(Disbursement) kaydini gosterir.</summary>
    public Guid? PaymentId { get; set; }
}

// ---------- Cek portfoyu (surum 2 hazirlik notu madde 3) ----------

public enum CheckDirection { Received, Issued }

public enum CheckStatus { InPortfolio, SubmittedForCollection, Collected, Endorsed, Bounced }

/// <summary>
/// Cek kaydinin kendisi cariye tutar yazmaz (ADR-004 ilkesi burada da gecerlidir).
/// Tahsil edildiginde otomatik bir Payment uretilir ve PaymentId'ye baglanir;
/// nakit girisi vade tarihinde sayilir (spesifikasyon O37).
/// </summary>
public sealed class Check : TenantEntity
{
    public Guid PartyId { get; set; }
    public CheckDirection Direction { get; set; }
    public string CheckNo { get; set; } = string.Empty;
    public string BankName { get; set; } = string.Empty;
    public DateOnly DueDate { get; set; }
    public decimal Amount { get; set; }
    public CheckStatus Status { get; set; } = CheckStatus.InPortfolio;
    /// <summary>Seri girilen cekleri gruplamak icin; tekil girilen cekte de doludur (kendi id'si).</summary>
    public Guid BatchId { get; set; }
    public Guid? PaymentId { get; set; }
    public string? StatusNote { get; set; }
}

// ---------- Denetim kaydi ----------

/// <summary>Append-only: uygulama rolunun UPDATE/DELETE hakki yoktur (guvenlik standardi 9).</summary>
public sealed class AuditEvent
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public Guid TenantId { get; set; }
    public Guid? UserId { get; set; }
    public string? UserName { get; set; }
    public DateTimeOffset OccurredAt { get; set; } = DateTimeOffset.UtcNow;
    public string Action { get; set; } = string.Empty;
    public string TargetType { get; set; } = string.Empty;
    public Guid? TargetId { get; set; }
    public string CorrelationId { get; set; } = string.Empty;
    public string? Summary { get; set; }
    /// <summary>Giris/cikis/kilit gibi olaylarda istek IP'si (guvenlik denetimi S-13).</summary>
    public string? IpAddress { get; set; }
}
