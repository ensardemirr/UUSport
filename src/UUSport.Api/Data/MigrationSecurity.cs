using Microsoft.EntityFrameworkCore.Migrations;

namespace UUSport.Api.Data;

/// <summary>
/// Her migration kendi olusturdugu tablolar icin bu yardimcilari cagirir (guvenlik
/// standardi bolum 4). Tablo listeleri migration'a ozeldir ve degistirilemez: paylasilan
/// bir "tum tablolar" listesi kullanmak, sifir bir veritabaninda eski migration'lar
/// calisirken henuz var olmayan tabloya RLS uygulamaya calisip patlar. Yeni tablo
/// eklendiginde bu dosyadaki eski sabitler DEGISTIRILMEZ; yeni migration kendi
/// ApplyRls(...)/RevokeUpdateDelete(...) cagrisini yapar.
/// </summary>
public static class MigrationSecurity
{
    public const string AppRole = "uusport_app";

    public static readonly string[] Schemas =
        ["core", "identity", "crm", "academy", "inventory", "billing", "finance", "integration", "audit"];

    /// <summary>20260925225410_InitialSchema'nin olusturdugu, RLS ile korunan tablolar. Dondurulmustur.</summary>
    private static readonly string[] InitialTenantTables =
    [
        "crm.party", "academy.athlete", "inventory.item", "inventory.stock_movement",
        "billing.trade_line", "billing.document", "billing.document_vat_line", "billing.document_link",
        "finance.cash_account", "finance.payment", "finance.payment_allocation", "finance.opening_balance",
        "finance.ledger_entry", "integration.import_batch", "integration.import_row", "audit.audit_event",
    ];

    /// <summary>20260925225410_InitialSchema'nin olusturdugu degistirilemez tablolar. Dondurulmustur.</summary>
    private static readonly string[] InitialAppendOnlyTables =
        ["finance.ledger_entry", "inventory.stock_movement", "audit.audit_event", "finance.payment_allocation"];

    /// <summary>
    /// 20260927143530_FixAppendOnlyGrants itibariyla var olan degistirilemez tablolar. Dondurulmustur:
    /// yeni append-only tablo ekleyen migration bu listeyi DEGISTIRMEZ, kendi listesini gecer.
    /// </summary>
    public static readonly string[] AppendOnlyTablesAsOf20260927 =
        ["finance.ledger_entry", "inventory.stock_movement", "audit.audit_event", "finance.payment_allocation"];

    /// <summary>
    /// Yeni migration'lar icin: sema yetkilerini verir ve ardindan verilen degistirilemez tablolardan
    /// UPDATE/DELETE/TRUNCATE'i yeniden alir. <paramref name="appendOnlyTables"/> o migration aninda var olan
    /// TUM append-only tablolari icermelidir (ör. AppendOnlyTablesAsOf20260927 + migration'in yenileri).
    /// </summary>
    public static void ApplySchemaGrants(MigrationBuilder mb, string[] appendOnlyTables)
    {
        ApplySchemaGrants(mb);
        RevokeUpdateDelete(mb, appendOnlyTables);
    }

    /// <summary>
    /// Sema bazli yetkiler. "ALL TABLES IN SCHEMA" o an semada var olan tablolara uygulanir.
    /// DIKKAT: append-only tablolar dahil her tabloya UPDATE verir. Yalniz gecmis migration'lar icin
    /// korunur; yeni migration'lar iki parametreli asiri yuklemeyi kullanir (20260926 hatasi: bu cagri
    /// append-only korumasini sifir veritabaninda geri aciyordu; SchemaGuardTests bunu yakalar).
    /// </summary>
    public static void ApplySchemaGrants(MigrationBuilder mb)
    {
        foreach (var schema in Schemas)
        {
            mb.Sql($"GRANT USAGE ON SCHEMA {schema} TO {AppRole};");
            mb.Sql($"GRANT SELECT, INSERT, UPDATE ON ALL TABLES IN SCHEMA {schema} TO {AppRole};");
        }

        // Identity tablolarinda token ve rol satirlari silinebilir.
        mb.Sql($"GRANT DELETE ON ALL TABLES IN SCHEMA identity TO {AppRole};");
        mb.Sql($"GRANT SELECT ON TABLE public.\"__EFMigrationsHistory\" TO {AppRole};");
    }

    /// <summary>
    /// 20261017 (ADR-012) itibariyla kiraci (sirket) verisi tasiyan TUM tablolar. Dondurulmustur: sonraki
    /// migration'lar yeni tablolari kendi ApplyRlsV2 cagrisina gecer, bu listeyi degistirmez.
    /// </summary>
    public static readonly string[] TenantTablesAsOf20261017 =
    [
        "crm.party", "academy.athlete", "inventory.item", "inventory.stock_movement", "inventory.supplier_item_code",
        "billing.trade_line", "billing.document", "billing.document_vat_line", "billing.document_link",
        "finance.cash_account", "finance.payment", "finance.payment_allocation", "finance.opening_balance",
        "finance.ledger_entry", "finance.loan", "finance.loan_installment", "finance.check_note",
        "integration.import_batch", "integration.import_row", "audit.audit_event",
    ];

    /// <summary>
    /// Coklu sirket politikasi (ADR-012 2.3): OKUMA secili sirket kumesi (`app.tenant_ids`, virgulle ayrilmis),
    /// YAZMA tam olarak bir sirket (`app.tenant_id`). Birlesik gorunumde yanlis sirkete kayit veritabani
    /// seviyesinde imkansizdir. Politika adi ayni kalir (tenant_isolation); SchemaGuardTests metni dogrular.
    /// </summary>
    public const string ReadPolicySql =
        "tenant_id = ANY (string_to_array(NULLIF(current_setting('app.tenant_ids', true), ''), ',')::uuid[])";

    public const string WritePolicySql =
        "tenant_id = NULLIF(current_setting('app.tenant_id', true), '')::uuid";

    public static void ApplyRlsV2(MigrationBuilder mb, params string[] tables)
    {
        foreach (var table in tables)
        {
            const string policy = "tenant_isolation";
            mb.Sql($"ALTER TABLE {table} ENABLE ROW LEVEL SECURITY;");
            mb.Sql($"ALTER TABLE {table} FORCE ROW LEVEL SECURITY;");
            mb.Sql($"DROP POLICY IF EXISTS {policy} ON {table};");
            mb.Sql($"""
                CREATE POLICY {policy} ON {table}
                  USING ({ReadPolicySql})
                  WITH CHECK ({WritePolicySql});
                """);
        }
    }

    /// <summary>
    /// Tek kiracili (v1) politika. Yalniz 20261017 oncesi migration'lar icindir; yeni tablolar ApplyRlsV2 kullanir.
    /// Verilen tablolarda RLS acar ve kiraci izolasyon politikasini kurar. Tekrar calistirilabilir.
    /// </summary>
    public static void ApplyRls(MigrationBuilder mb, params string[] tables)
    {
        foreach (var table in tables)
        {
            const string policy = "tenant_isolation";
            mb.Sql($"ALTER TABLE {table} ENABLE ROW LEVEL SECURITY;");
            mb.Sql($"ALTER TABLE {table} FORCE ROW LEVEL SECURITY;");
            mb.Sql($"DROP POLICY IF EXISTS {policy} ON {table};");
            mb.Sql($"""
                CREATE POLICY {policy} ON {table}
                  USING (tenant_id = NULLIF(current_setting('app.tenant_id', true), '')::uuid)
                  WITH CHECK (tenant_id = NULLIF(current_setting('app.tenant_id', true), '')::uuid);
                """);
        }
    }

    /// <summary>Verilen tablolari degistirilemez (append-only) yapar: uygulama rolunden UPDATE/DELETE/TRUNCATE alinir.</summary>
    public static void RevokeUpdateDelete(MigrationBuilder mb, params string[] tables)
    {
        foreach (var table in tables)
            mb.Sql($"REVOKE UPDATE, DELETE, TRUNCATE ON TABLE {table} FROM {AppRole};");
    }

    /// <summary>Yalniz 20260925225410_InitialSchema'nin cagrisi icindir; listesi dondurulmustur, degistirilmez.</summary>
    public static void Apply(MigrationBuilder mb)
    {
        ApplySchemaGrants(mb);
        RevokeUpdateDelete(mb, InitialAppendOnlyTables);
        ApplyRls(mb, InitialTenantTables);
    }
}
