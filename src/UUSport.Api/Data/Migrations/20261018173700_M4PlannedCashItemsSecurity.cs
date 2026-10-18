using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace UUSport.Api.Data.Migrations
{
    /// <summary>
    /// `finance.planned_cash_item` icin uygulama rolu yetkileri ve RLS v2 (okuma secili sirketler, yazma tek
    /// sirket; ADR-012 2.3). Sema degisikligi onceki migration'dadir (M4PlannedCashItems); bu dosya yalniz
    /// guvenligi kurar, model degistirmez. Dondurulmus tablo listeleri degismez (devir notu bolum 4).
    /// </summary>
    [DbContext(typeof(AppDbContext))]
    [Migration("20261018173700_M4PlannedCashItemsSecurity")]
    public partial class M4PlannedCashItemsSecurity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            MigrationSecurity.ApplySchemaGrants(migrationBuilder, MigrationSecurity.AppendOnlyTablesAsOf20260927);
            MigrationSecurity.ApplyRlsV2(migrationBuilder, "finance.planned_cash_item");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP POLICY IF EXISTS tenant_isolation ON finance.planned_cash_item;");
            migrationBuilder.Sql("ALTER TABLE finance.planned_cash_item NO FORCE ROW LEVEL SECURITY;");
            migrationBuilder.Sql("ALTER TABLE finance.planned_cash_item DISABLE ROW LEVEL SECURITY;");
        }
    }
}
