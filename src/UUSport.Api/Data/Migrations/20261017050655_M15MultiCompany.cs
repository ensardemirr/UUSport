using System;
using System.Linq;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace UUSport.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class M15MultiCompany : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "code",
                schema: "core",
                table: "tenant",
                type: "character varying(8)",
                maxLength: 8,
                nullable: false,
                defaultValue: "",
                collation: "C");

            migrationBuilder.AddColumn<string>(
                name: "color_key",
                schema: "core",
                table: "tenant",
                type: "character varying(30)",
                maxLength: 30,
                nullable: true,
                collation: "C");

            migrationBuilder.AddColumn<bool>(
                name: "is_active",
                schema: "core",
                table: "tenant",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<string>(
                name: "legal_name",
                schema: "core",
                table: "tenant",
                type: "character varying(300)",
                maxLength: 300,
                nullable: true);

            migrationBuilder.AddColumn<short>(
                name: "sort_order",
                schema: "core",
                table: "tenant",
                type: "smallint",
                nullable: false,
                defaultValue: (short)0);

            migrationBuilder.AddColumn<string>(
                name: "tax_number",
                schema: "core",
                table: "tenant",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true,
                collation: "C");

            migrationBuilder.AddColumn<string>(
                name: "tax_office",
                schema: "core",
                table: "tenant",
                type: "character varying(120)",
                maxLength: 120,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "related_tenant_id",
                schema: "crm",
                table: "party",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "is_group_admin",
                schema: "identity",
                table: "app_user",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "company_membership",
                schema: "identity",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    role = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false, collation: "C"),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    granted_keys = table.Column<string[]>(type: "text[]", nullable: false),
                    denied_keys = table.Column<string[]>(type: "text[]", nullable: false),
                    granted_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    granted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    revoked_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_company_membership", x => x.id);
                    table.CheckConstraint("ck_company_membership__role_allowed", "role IN ('Yonetici', 'Muhasebe', 'Ortak')");
                    table.ForeignKey(
                        name: "fk_company_membership_app_user_user_id",
                        column: x => x.user_id,
                        principalSchema: "identity",
                        principalTable: "app_user",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_company_membership_tenant_tenant_id",
                        column: x => x.tenant_id,
                        principalSchema: "core",
                        principalTable: "tenant",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "supplier_item_code",
                schema: "inventory",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    item_id = table.Column<Guid>(type: "uuid", nullable: false),
                    supplier_party_id = table.Column<Guid>(type: "uuid", nullable: false),
                    supplier_code = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false, collation: "C"),
                    barcode = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true, collation: "C"),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_supplier_item_code", x => x.id);
                    table.ForeignKey(
                        name: "fk_supplier_item_code_item_item_id",
                        column: x => x.item_id,
                        principalSchema: "inventory",
                        principalTable: "item",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_supplier_item_code_party_supplier_party_id",
                        column: x => x.supplier_party_id,
                        principalSchema: "crm",
                        principalTable: "party",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_supplier_item_code_tenant_tenant_id",
                        column: x => x.tenant_id,
                        principalSchema: "core",
                        principalTable: "tenant",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            // ---- ADR-012 veri tasima 1: sirket kodlari. Ilk (en eski) kiraci UUSPORT = 'UUS' olur; test
            // veritabanindaki diger kiracilar sirali gecici kod alir. Benzersiz indeks bundan SONRA kurulur.
            migrationBuilder.Sql("""
                UPDATE core.tenant SET code = 'UUS'
                 WHERE id = (SELECT id FROM core.tenant ORDER BY created_at, id LIMIT 1);
                WITH numbered AS (
                    SELECT id, row_number() OVER (ORDER BY created_at, id) AS rn
                      FROM core.tenant WHERE code = '')
                UPDATE core.tenant t SET code = 'T' || lpad(n.rn::text, 5, '0')
                  FROM numbered n WHERE n.id = t.id;
                """);

            migrationBuilder.CreateIndex(
                name: "uq_tenant__code",
                schema: "core",
                table: "tenant",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_party_related_tenant_id",
                schema: "crm",
                table: "party",
                column: "related_tenant_id");

            migrationBuilder.CreateIndex(
                name: "uq_party__tenant_related_tenant",
                schema: "crm",
                table: "party",
                columns: new[] { "tenant_id", "related_tenant_id" },
                unique: true,
                filter: "related_tenant_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_company_membership__tenant",
                schema: "identity",
                table: "company_membership",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "uq_company_membership__user_tenant_active",
                schema: "identity",
                table: "company_membership",
                columns: new[] { "user_id", "tenant_id" },
                unique: true,
                filter: "is_active");

            migrationBuilder.CreateIndex(
                name: "ix_supplier_item_code__tenant_barcode",
                schema: "inventory",
                table: "supplier_item_code",
                columns: new[] { "tenant_id", "barcode" });

            migrationBuilder.CreateIndex(
                name: "ix_supplier_item_code_item_id",
                schema: "inventory",
                table: "supplier_item_code",
                column: "item_id");

            migrationBuilder.CreateIndex(
                name: "ix_supplier_item_code_supplier_party_id",
                schema: "inventory",
                table: "supplier_item_code",
                column: "supplier_party_id");

            migrationBuilder.CreateIndex(
                name: "uq_supplier_item_code__tenant_supplier_code",
                schema: "inventory",
                table: "supplier_item_code",
                columns: new[] { "tenant_id", "supplier_party_id", "supplier_code" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "fk_party_tenant_related_tenant_id",
                schema: "crm",
                table: "party",
                column: "related_tenant_id",
                principalSchema: "core",
                principalTable: "tenant",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            // ---- ADR-012 veri tasima 2: Identity rolleri sirket uyeligine donusur; her Yonetici tek
            // sirketli donemde tam yetkiliydi, grup yoneticisi olur. Identity rol tablosu artik yetki
            // kaynagi degildir (silinmez; tarihsel kayit).
            migrationBuilder.Sql("""
                INSERT INTO identity.company_membership
                    (id, user_id, tenant_id, role, is_active, granted_keys, denied_keys, granted_at)
                SELECT DISTINCT ON (u.id) gen_random_uuid(), u.id, u.tenant_id, r.name, u.is_active, '{}', '{}', now()
                  FROM identity.app_user u
                  JOIN identity.user_role ur ON ur.user_id = u.id
                  JOIN identity.app_role r ON r.id = ur.role_id
                 WHERE r.name IN ('Yonetici', 'Muhasebe')
                 ORDER BY u.id, CASE WHEN r.name = 'Yonetici' THEN 0 ELSE 1 END;
                UPDATE identity.app_user u SET is_group_admin = true
                 WHERE EXISTS (SELECT 1 FROM identity.user_role ur JOIN identity.app_role r ON r.id = ur.role_id
                                WHERE ur.user_id = u.id AND r.name = 'Yonetici');
                """);

            // ---- ADR-012 veri tasima 3: mevcut cari/urun kodlarina sirket on eki (2.8). Migration rolu
            // FORCE RLS altinda satir goremedigi icin zorlama gecici olarak kaldirilir; ApplyRlsV2 yeniden kurar.
            migrationBuilder.Sql("""
                ALTER TABLE crm.party NO FORCE ROW LEVEL SECURITY;
                ALTER TABLE inventory.item NO FORCE ROW LEVEL SECURITY;
                UPDATE crm.party p SET code = t.code || '-' || p.code
                  FROM core.tenant t WHERE t.id = p.tenant_id AND p.code NOT LIKE t.code || '-%';
                UPDATE inventory.item i SET code = t.code || '-' || i.code
                  FROM core.tenant t WHERE t.id = i.tenant_id AND i.code NOT LIKE t.code || '-%';
                """);

            // Yeni tablolar icin yetkiler (append-only listesi degismedi) ve tum sirket tablolarinda RLS v2:
            // okuma secili sirket kumesi, yazma tek sirket (ADR-012 2.3).
            MigrationSecurity.ApplySchemaGrants(migrationBuilder, MigrationSecurity.AppendOnlyTablesAsOf20260927);
            MigrationSecurity.ApplyRlsV2(migrationBuilder, MigrationSecurity.TenantTablesAsOf20261017);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Geri alma yalniz semayi geri dondurur; politika v1'e ve kod on ekleri eski haline
            // otomatik donmez (operasyon proseduru: veritabani geri donusu yedekten yapilir).
            MigrationSecurity.ApplyRls(migrationBuilder, MigrationSecurity.TenantTablesAsOf20261017
                .Where(t => t != "inventory.supplier_item_code").ToArray());

            migrationBuilder.DropForeignKey(
                name: "fk_party_tenant_related_tenant_id",
                schema: "crm",
                table: "party");

            migrationBuilder.DropTable(
                name: "company_membership",
                schema: "identity");

            migrationBuilder.DropTable(
                name: "supplier_item_code",
                schema: "inventory");

            migrationBuilder.DropIndex(
                name: "uq_tenant__code",
                schema: "core",
                table: "tenant");

            migrationBuilder.DropIndex(
                name: "ix_party_related_tenant_id",
                schema: "crm",
                table: "party");

            migrationBuilder.DropIndex(
                name: "uq_party__tenant_related_tenant",
                schema: "crm",
                table: "party");

            migrationBuilder.DropColumn(
                name: "code",
                schema: "core",
                table: "tenant");

            migrationBuilder.DropColumn(
                name: "color_key",
                schema: "core",
                table: "tenant");

            migrationBuilder.DropColumn(
                name: "is_active",
                schema: "core",
                table: "tenant");

            migrationBuilder.DropColumn(
                name: "legal_name",
                schema: "core",
                table: "tenant");

            migrationBuilder.DropColumn(
                name: "sort_order",
                schema: "core",
                table: "tenant");

            migrationBuilder.DropColumn(
                name: "tax_number",
                schema: "core",
                table: "tenant");

            migrationBuilder.DropColumn(
                name: "tax_office",
                schema: "core",
                table: "tenant");

            migrationBuilder.DropColumn(
                name: "related_tenant_id",
                schema: "crm",
                table: "party");

            migrationBuilder.DropColumn(
                name: "is_group_admin",
                schema: "identity",
                table: "app_user");
        }
    }
}
