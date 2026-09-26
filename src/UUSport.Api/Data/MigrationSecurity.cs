using Microsoft.EntityFrameworkCore.Migrations;

namespace UUSport.Api.Data;

/// <summary>
/// Her migration'in sonunda cagrilir: rol yetkileri ve RLS politikalari yeniden uygulanir
/// (guvenlik standardi bolum 4). Tekrar calistirilabilir.
/// </summary>
public static class MigrationSecurity
{
    public const string AppRole = "uusport_app";

    public static readonly string[] Schemas =
        ["core", "identity", "crm", "academy", "inventory", "billing", "finance", "integration", "audit"];

    /// <summary>tenant_id tasiyan ve RLS ile korunan tablolar.</summary>
    public static readonly string[] TenantTables =
    [
        "crm.party", "academy.athlete", "inventory.item", "inventory.stock_movement",
        "billing.trade_line", "billing.document", "billing.document_vat_line", "billing.document_link",
        "finance.cash_account", "finance.payment", "finance.payment_allocation", "finance.opening_balance",
        "finance.ledger_entry", "integration.import_batch", "integration.import_row", "audit.audit_event",
    ];

    /// <summary>Degistirilemez tablolar: uygulama rolu yalniz ekleyip okuyabilir.</summary>
    public static readonly string[] AppendOnlyTables =
        ["finance.ledger_entry", "inventory.stock_movement", "audit.audit_event", "finance.payment_allocation"];

    public static void Apply(MigrationBuilder mb)
    {
        foreach (var schema in Schemas)
        {
            mb.Sql($"GRANT USAGE ON SCHEMA {schema} TO {AppRole};");
            mb.Sql($"GRANT SELECT, INSERT, UPDATE ON ALL TABLES IN SCHEMA {schema} TO {AppRole};");
        }

        // Identity tablolarinda token ve rol satirlari silinebilir.
        mb.Sql($"GRANT DELETE ON ALL TABLES IN SCHEMA identity TO {AppRole};");
        mb.Sql($"GRANT SELECT ON TABLE public.\"__EFMigrationsHistory\" TO {AppRole};");

        foreach (var table in AppendOnlyTables)
            mb.Sql($"REVOKE UPDATE, DELETE, TRUNCATE ON TABLE {table} FROM {AppRole};");

        foreach (var table in TenantTables)
        {
            var policy = "tenant_isolation";
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
}
