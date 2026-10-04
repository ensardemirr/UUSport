using System.Text;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using UUSport.Api.Domain;

namespace UUSport.Api.Data;

/// <summary>
/// dataProtection null olabilir (dogrudan `new AppDbContext(options)` kullanan testler/tasarim
/// zamani araclar icin); DI ortaminda Program.cs'teki AddDataProtection ile doldurulur.
/// </summary>
public sealed class AppDbContext(DbContextOptions<AppDbContext> options, IDataProtectionProvider? dataProtection = null)
    : IdentityDbContext<AppUser, AppRole, Guid>(options)
{
    public DbSet<Tenant> Tenants => Set<Tenant>();
    public DbSet<Party> Parties => Set<Party>();
    public DbSet<Athlete> Athletes => Set<Athlete>();
    public DbSet<Item> Items => Set<Item>();
    public DbSet<StockMovement> StockMovements => Set<StockMovement>();
    public DbSet<TradeLine> TradeLines => Set<TradeLine>();
    public DbSet<Document> Documents => Set<Document>();
    public DbSet<DocumentVatLine> DocumentVatLines => Set<DocumentVatLine>();
    public DbSet<DocumentLink> DocumentLinks => Set<DocumentLink>();
    public DbSet<CashAccount> CashAccounts => Set<CashAccount>();
    public DbSet<Loan> Loans => Set<Loan>();
    public DbSet<LoanInstallment> LoanInstallments => Set<LoanInstallment>();
    public DbSet<Check> Checks => Set<Check>();
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<PaymentAllocation> PaymentAllocations => Set<PaymentAllocation>();
    public DbSet<OpeningBalance> OpeningBalances => Set<OpeningBalance>();
    public DbSet<LedgerEntry> LedgerEntries => Set<LedgerEntry>();
    public DbSet<ImportBatch> ImportBatches => Set<ImportBatch>();
    public DbSet<ImportRow> ImportRows => Set<ImportRow>();
    public DbSet<AuditEvent> AuditEvents => Set<AuditEvent>();

    protected override void ConfigureConventions(ModelConfigurationBuilder b)
    {
        // ADR-002: kesinlesmis para numeric(18,2); ozel alanlar asagida ayrica ayarlanir.
        b.Properties<decimal>().HavePrecision(18, 2);
        b.Properties<string>().HaveMaxLength(400);
    }

    protected override void OnModelCreating(ModelBuilder b)
    {
        base.OnModelCreating(b);

        // ---- identity ----
        b.Entity<AppUser>(e =>
        {
            e.ToTable("app_user", "identity");
            e.Property(x => x.UserName).UseCollation("C");
            e.Property(x => x.NormalizedUserName).UseCollation("C");
            e.Property(x => x.Email).UseCollation("C");
            e.Property(x => x.NormalizedEmail).UseCollation("C");
            e.Property(x => x.FullName).HasMaxLength(200);
            e.Property(x => x.MaxDiscountPercent).HasPrecision(9, 4);
            e.Property(x => x.PasswordHash).HasMaxLength(1000);
            e.Property(x => x.SecurityStamp).HasMaxLength(1000);
            e.Property(x => x.ConcurrencyStamp).HasMaxLength(1000);
            e.HasOne<Tenant>().WithMany().HasForeignKey(x => x.TenantId).OnDelete(DeleteBehavior.Restrict);
        });
        b.Entity<AppRole>().ToTable("app_role", "identity");
        b.Entity<IdentityUserRole<Guid>>().ToTable("user_role", "identity");
        b.Entity<IdentityUserClaim<Guid>>().ToTable("user_claim", "identity");
        b.Entity<IdentityUserLogin<Guid>>().ToTable("user_login", "identity");
        var tokenEntity = b.Entity<IdentityUserToken<Guid>>();
        tokenEntity.ToTable("user_token", "identity");
        // Sifreli deger duz metinden belirgin uzun olur (IV + HMAC + base64); genel 400 karakter
        // kuralindan (ConfigureConventions) ayrik, kirpilma riskine karsi genis tutulur.
        tokenEntity.Property(t => t.Value).HasMaxLength(2000);
        if (dataProtection is not null)
        {
            // TOTP anahtari ve kurtarma kodlari bu tabloda tutulur (S-15); Data Protection ile
            // sifrelenir. Eski (M0 oncesi) duz metin satirlar da okunabilir kalir: cozulemezse
            // ham deger dondurulur, bir sonraki yazimda otomatik sifrelenir.
            var protector = dataProtection.CreateProtector("UUSport.Identity.UserToken.v1");
            var converter = new ValueConverter<string, string>(
                v => protector.Protect(v),
                v => TryUnprotect(protector, v));
            tokenEntity.Property(t => t.Value).HasConversion(converter!);
        }
        b.Entity<IdentityRoleClaim<Guid>>().ToTable("role_claim", "identity");

        b.Entity<Tenant>(e =>
        {
            e.ToTable("tenant", "core");
            e.Property(x => x.Id).ValueGeneratedNever();
            e.Property(x => x.Name).HasMaxLength(200);
        });

        // ---- crm ----
        b.Entity<Party>(e =>
        {
            Tenant(e, "party", "crm");
            e.Property(x => x.Code).HasMaxLength(40).UseCollation("C");
            e.Property(x => x.Name).HasMaxLength(200);
            e.Property(x => x.Kind).HasConversion(Snake<PartyKind>()).HasMaxLength(30);
            e.Property(x => x.Phone).HasMaxLength(30).UseCollation("C");
            e.Property(x => x.Email).HasMaxLength(200).UseCollation("C");
            e.Property(x => x.TaxNumber).HasMaxLength(20).UseCollation("C");
            e.Property(x => x.AccountCode).HasMaxLength(40).UseCollation("C");
            e.Property(x => x.Notes).HasMaxLength(2000);
            e.Property(x => x.SearchKey).HasMaxLength(400).UseCollation("C");
            e.HasIndex(x => new { x.TenantId, x.Code }).IsUnique().HasDatabaseName("uq_party__tenant_code");
            e.HasIndex(x => new { x.TenantId, x.Phone }).HasDatabaseName("ix_party__tenant_phone");
        });

        b.Entity<Athlete>(e =>
        {
            Tenant(e, "athlete", "academy");
            e.Property(x => x.FirstName).HasMaxLength(100);
            e.Property(x => x.LastName).HasMaxLength(100);
            e.Property(x => x.GroupName).HasMaxLength(100);
            e.Property(x => x.SearchKey).HasMaxLength(400).UseCollation("C");
            e.HasOne<Party>().WithMany().HasForeignKey(x => x.PartyId).OnDelete(DeleteBehavior.Restrict);
        });

        // ---- inventory ----
        b.Entity<Item>(e =>
        {
            Tenant(e, "item", "inventory");
            e.Property(x => x.Code).HasMaxLength(40).UseCollation("C");
            e.Property(x => x.Name).HasMaxLength(200);
            e.Property(x => x.Kind).HasConversion(Snake<ItemKind>()).HasMaxLength(30);
            e.Property(x => x.Unit).HasMaxLength(20);
            e.Property(x => x.VatRate).HasPrecision(9, 4);
            e.Property(x => x.AccountCode).HasMaxLength(40).UseCollation("C");
            e.Property(x => x.SearchKey).HasMaxLength(400).UseCollation("C");
            e.HasIndex(x => new { x.TenantId, x.Code }).IsUnique().HasDatabaseName("uq_item__tenant_code");
            e.ToTable(t => t.HasCheckConstraint("ck_item__price_nonnegative", "unit_price >= 0"));
        });

        b.Entity<StockMovement>(e =>
        {
            Tenant(e, "stock_movement", "inventory");
            e.Property(x => x.Quantity).HasPrecision(18, 4);
            e.Property(x => x.SourceType).HasMaxLength(40).UseCollation("C");
            e.HasOne<Item>().WithMany().HasForeignKey(x => x.ItemId).OnDelete(DeleteBehavior.Restrict);
            e.HasIndex(x => new { x.TenantId, x.SourceType, x.SourceId }).IsUnique()
                .HasDatabaseName("uq_stock_movement__tenant_source");
            e.ToTable(t => t.HasCheckConstraint("ck_stock_movement__quantity_nonzero", "quantity <> 0"));
        });

        // ---- billing ----
        b.Entity<TradeLine>(e =>
        {
            Tenant(e, "trade_line", "billing");
            e.Property(x => x.Direction).HasConversion(Snake<TradeDirection>()).HasMaxLength(20);
            e.Property(x => x.Quantity).HasPrecision(18, 4);
            e.Property(x => x.DocumentedQuantity).HasPrecision(18, 4);
            e.Property(x => x.UnitPrice).HasPrecision(18, 4);
            e.Property(x => x.DiscountPercent).HasPrecision(9, 4);
            e.Property(x => x.VatRate).HasPrecision(9, 4);
            e.Property(x => x.NetAmount).HasPrecision(18, 6);
            e.Property(x => x.VatAmount).HasPrecision(18, 6);
            e.Property(x => x.Description).HasMaxLength(500);
            e.HasOne<Party>().WithMany().HasForeignKey(x => x.PartyId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne<Item>().WithMany().HasForeignKey(x => x.ItemId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne<TradeLine>().WithMany().HasForeignKey(x => x.ReversesLineId).OnDelete(DeleteBehavior.Restrict);
            e.HasIndex(x => new { x.TenantId, x.PartyId, x.OccurredOn });
            e.ToTable(t =>
            {
                t.HasCheckConstraint("ck_trade_line__quantity_nonzero", "quantity <> 0");
                t.HasCheckConstraint("ck_trade_line__documented_within_quantity",
                    "abs(documented_quantity) <= abs(quantity) AND (documented_quantity = 0 OR sign(documented_quantity) = sign(quantity))");
                t.HasCheckConstraint("ck_trade_line__reversal_is_negative",
                    "(reverses_line_id IS NULL AND quantity > 0) OR (reverses_line_id IS NOT NULL AND quantity < 0)");
                t.HasCheckConstraint("ck_trade_line__discount_range", "discount_percent >= 0 AND discount_percent <= 100");
            });
        });

        b.Entity<Document>(e =>
        {
            Tenant(e, "document", "billing");
            e.Property(x => x.Direction).HasConversion(Snake<TradeDirection>()).HasMaxLength(20);
            e.Property(x => x.Type).HasConversion(Snake<DocumentType>()).HasMaxLength(20);
            e.Property(x => x.Status).HasConversion(Snake<DocumentStatus>()).HasMaxLength(20);
            e.Property(x => x.DocumentNo).HasMaxLength(60).UseCollation("C");
            e.Property(x => x.Note).HasMaxLength(1000);
            e.Property(x => x.CancelReason).HasMaxLength(1000);
            e.HasOne<Party>().WithMany().HasForeignKey(x => x.PartyId).OnDelete(DeleteBehavior.Restrict);
            e.HasMany(x => x.VatLines).WithOne().HasForeignKey(x => x.DocumentId).OnDelete(DeleteBehavior.Restrict);
            e.HasMany(x => x.Links).WithOne().HasForeignKey(x => x.DocumentId).OnDelete(DeleteBehavior.Restrict);
            // Satis faturasi numarasi (verende benzersiz); alis faturasi tedarikci basina benzersiz.
            e.HasIndex(x => new { x.TenantId, x.DocumentNo }).IsUnique()
                .HasFilter("direction = 'sale' AND status = 'issued'")
                .HasDatabaseName("uq_document__tenant_sale_no");
            e.HasIndex(x => new { x.TenantId, x.PartyId, x.DocumentNo }).IsUnique()
                .HasFilter("direction = 'purchase' AND status = 'issued'")
                .HasDatabaseName("uq_document__tenant_party_purchase_no");
        });

        b.Entity<DocumentVatLine>(e =>
        {
            Tenant(e, "document_vat_line", "billing");
            e.Property(x => x.VatRate).HasPrecision(9, 4);
        });

        b.Entity<DocumentLink>(e =>
        {
            Tenant(e, "document_link", "billing");
            e.Property(x => x.Quantity).HasPrecision(18, 4);
            e.HasOne<TradeLine>().WithMany().HasForeignKey(x => x.TradeLineId).OnDelete(DeleteBehavior.Restrict);
            e.ToTable(t => t.HasCheckConstraint("ck_document_link__quantity_nonzero", "quantity <> 0"));
        });

        // ---- finance ----
        b.Entity<CashAccount>(e =>
        {
            Tenant(e, "cash_account", "finance");
            e.Property(x => x.Name).HasMaxLength(120);
            e.Property(x => x.Kind).HasConversion(Snake<CashAccountKind>()).HasMaxLength(20);
            // Parent, uygulama katmaninda dogrulanir (Kind=Bank olmali); DB kisitiyla
            // capraz-satir kural zorlanamadigi icin CatalogEndpoints.ValidateHierarchy'ye bakin.
            e.HasOne<CashAccount>().WithMany().HasForeignKey(x => x.ParentAccountId).OnDelete(DeleteBehavior.Restrict);
        });

        b.Entity<Loan>(e =>
        {
            Tenant(e, "loan", "finance");
            e.Property(x => x.Name).HasMaxLength(200);
            e.Property(x => x.PrincipalAmount).HasPrecision(18, 2);
            e.Property(x => x.Notes).HasMaxLength(1000);
            e.HasOne<Party>().WithMany().HasForeignKey(x => x.PartyId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne<CashAccount>().WithMany().HasForeignKey(x => x.CashAccountId).OnDelete(DeleteBehavior.Restrict);
            e.ToTable(t => t.HasCheckConstraint("ck_loan__principal_positive", "principal_amount > 0"));
        });

        b.Entity<LoanInstallment>(e =>
        {
            Tenant(e, "loan_installment", "finance");
            e.Property(x => x.PrincipalAmount).HasPrecision(18, 2);
            e.Property(x => x.InterestAmount).HasPrecision(18, 2);
            e.Property(x => x.FeeAmount).HasPrecision(18, 2);
            e.HasOne<Loan>().WithMany().HasForeignKey(x => x.LoanId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne<Payment>().WithMany().HasForeignKey(x => x.PaymentId).OnDelete(DeleteBehavior.Restrict);
            e.HasIndex(x => new { x.LoanId, x.InstallmentNo }).IsUnique().HasDatabaseName("uq_loan_installment__loan_no");
            e.ToTable(t =>
            {
                t.HasCheckConstraint("ck_loan_installment__amounts_nonnegative",
                    "principal_amount >= 0 AND interest_amount >= 0 AND fee_amount >= 0");
                t.HasCheckConstraint("ck_loan_installment__no_positive", "installment_no > 0");
            });
        });

        b.Entity<Check>(e =>
        {
            Tenant(e, "check_note", "finance");
            e.Property(x => x.CheckNo).HasMaxLength(60).UseCollation("C");
            e.Property(x => x.BankName).HasMaxLength(120);
            e.Property(x => x.Amount).HasPrecision(18, 2);
            e.Property(x => x.Direction).HasConversion(Snake<CheckDirection>()).HasMaxLength(20);
            e.Property(x => x.Status).HasConversion(Snake<CheckStatus>()).HasMaxLength(30);
            e.Property(x => x.StatusNote).HasMaxLength(500);
            e.HasOne<Party>().WithMany().HasForeignKey(x => x.PartyId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne<Payment>().WithMany().HasForeignKey(x => x.PaymentId).OnDelete(DeleteBehavior.Restrict);
            e.HasIndex(x => new { x.TenantId, x.BatchId }).HasDatabaseName("ix_check_note__tenant_batch");
            e.ToTable(t => t.HasCheckConstraint("ck_check_note__amount_positive", "amount > 0"));
        });

        b.Entity<Payment>(e =>
        {
            Tenant(e, "payment", "finance");
            e.Property(x => x.Direction).HasConversion(Snake<PaymentDirection>()).HasMaxLength(20);
            e.Property(x => x.Method).HasConversion(Snake<PaymentMethod>()).HasMaxLength(20);
            e.Property(x => x.Reference).HasMaxLength(120);
            e.Property(x => x.Description).HasMaxLength(500);
            e.HasOne<Party>().WithMany().HasForeignKey(x => x.PartyId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne<CashAccount>().WithMany().HasForeignKey(x => x.CashAccountId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne<Payment>().WithMany().HasForeignKey(x => x.ReversesPaymentId).OnDelete(DeleteBehavior.Restrict);
            e.HasIndex(x => x.ReversesPaymentId).IsUnique().HasDatabaseName("uq_payment__reverses_payment");
            e.ToTable(t => t.HasCheckConstraint("ck_payment__amount_positive", "amount > 0"));
        });

        b.Entity<PaymentAllocation>(e =>
        {
            Tenant(e, "payment_allocation", "finance");
            e.HasOne<Payment>().WithMany().HasForeignKey(x => x.PaymentId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne<TradeLine>().WithMany().HasForeignKey(x => x.TradeLineId).OnDelete(DeleteBehavior.Restrict);
            e.HasIndex(x => x.ReversesAllocationId).IsUnique().HasDatabaseName("uq_payment_allocation__reverses");
            e.ToTable(t => t.HasCheckConstraint("ck_payment_allocation__amount_nonzero", "amount <> 0"));
        });

        b.Entity<OpeningBalance>(e =>
        {
            Tenant(e, "opening_balance", "finance");
            e.Property(x => x.Status).HasConversion(Snake<OpeningBalanceStatus>()).HasMaxLength(20);
            e.Property(x => x.Note).HasMaxLength(500);
            e.HasOne<Party>().WithMany().HasForeignKey(x => x.PartyId).OnDelete(DeleteBehavior.Restrict);
            e.ToTable(t => t.HasCheckConstraint("ck_opening_balance__amount_nonzero", "amount <> 0"));
        });

        b.Entity<LedgerEntry>(e =>
        {
            Tenant(e, "ledger_entry", "finance");
            e.Property(x => x.SourceType).HasMaxLength(40).UseCollation("C");
            e.Property(x => x.Description).HasMaxLength(500);
            e.HasOne<Party>().WithMany().HasForeignKey(x => x.PartyId).OnDelete(DeleteBehavior.Restrict);
            e.HasIndex(x => new { x.TenantId, x.SourceType, x.SourceId }).IsUnique()
                .HasDatabaseName("uq_ledger_entry__tenant_source");
            e.HasIndex(x => new { x.TenantId, x.PartyId, x.OccurredOn })
                .HasDatabaseName("ix_ledger_entry__tenant_party_occurred_on");
            var sources = string.Join(", ", LedgerSources.All.Select(s => $"'{s}'"));
            e.ToTable(t =>
            {
                // ADR-004: belge (document) kaynak olarak kabul edilmez.
                t.HasCheckConstraint("ck_ledger_entry__source_type_allowed", $"source_type IN ({sources})");
                t.HasCheckConstraint("ck_ledger_entry__one_side",
                    "debit >= 0 AND credit >= 0 AND ((debit > 0) <> (credit > 0))");
            });
        });

        // ---- integration ----
        b.Entity<ImportBatch>(e =>
        {
            Tenant(e, "import_batch", "integration");
            e.Property(x => x.Kind).HasMaxLength(30).UseCollation("C");
            e.Property(x => x.FileName).HasMaxLength(260);
            e.Property(x => x.FileSha256).HasMaxLength(64).UseCollation("C");
            e.Property(x => x.SheetName).HasMaxLength(100);
            e.Property(x => x.MappingJson).HasColumnType("jsonb");
            e.Property(x => x.Status).HasConversion(Snake<ImportStatus>()).HasMaxLength(20);
            e.HasIndex(x => new { x.TenantId, x.Kind, x.FileSha256 }).IsUnique()
                .HasFilter("status = 'applied'")
                .HasDatabaseName("uq_import_batch__tenant_kind_hash_applied");
        });

        b.Entity<ImportRow>(e =>
        {
            Tenant(e, "import_row", "integration");
            e.Property(x => x.DataJson).HasColumnType("jsonb");
            e.Property(x => x.Errors).HasMaxLength(2000);
            e.Property(x => x.Warnings).HasMaxLength(2000);
            e.HasOne<ImportBatch>().WithMany().HasForeignKey(x => x.BatchId).OnDelete(DeleteBehavior.Restrict);
            e.HasIndex(x => new { x.BatchId, x.RowNumber }).IsUnique();
        });

        // ---- audit ----
        b.Entity<AuditEvent>(e =>
        {
            e.ToTable("audit_event", "audit");
            e.Property(x => x.Id).ValueGeneratedNever();
            e.Property(x => x.UserName).HasMaxLength(200);
            e.Property(x => x.Action).HasMaxLength(80).UseCollation("C");
            e.Property(x => x.TargetType).HasMaxLength(80).UseCollation("C");
            e.Property(x => x.CorrelationId).HasMaxLength(100);
            e.Property(x => x.Summary).HasColumnType("jsonb");
            e.HasOne<Tenant>().WithMany().HasForeignKey(x => x.TenantId).OnDelete(DeleteBehavior.Restrict);
            e.HasIndex(x => new { x.TenantId, x.OccurredAt });
        });
    }

    private static void Tenant<T>(EntityTypeBuilder<T> e, string table, string schema) where T : TenantEntity
    {
        e.ToTable(table, schema);
        e.Property(x => x.Id).ValueGeneratedNever();
        e.HasOne<Tenant>().WithMany().HasForeignKey(x => x.TenantId).OnDelete(DeleteBehavior.Restrict);
    }

    /// <summary>Enum degerleri veritabaninda lower_snake_case metin olarak saklanir (PostgreSQL standardi).</summary>
    private static ValueConverter<T, string> Snake<T>() where T : struct, Enum =>
        new(v => ToSnake(v.ToString()), v => Enum.Parse<T>(FromSnake(v)));

    private static string TryUnprotect(IDataProtector protector, string value)
    {
        try { return protector.Unprotect(value); }
        catch (Exception) { return value; }
    }

    public static string ToSnake(string name)
    {
        var sb = new StringBuilder(name.Length + 4);
        for (var i = 0; i < name.Length; i++)
        {
            var c = name[i];
            if (char.IsUpper(c) && i > 0) sb.Append('_');
            sb.Append(char.ToLowerInvariant(c));
        }
        return sb.ToString();
    }

    private static string FromSnake(string value) =>
        string.Concat(value.Split('_').Select(p => p.Length == 0 ? p : char.ToUpperInvariant(p[0]) + p[1..]));
}
