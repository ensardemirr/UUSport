using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace UUSport.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "identity");

            migrationBuilder.EnsureSchema(
                name: "academy");

            migrationBuilder.EnsureSchema(
                name: "audit");

            migrationBuilder.EnsureSchema(
                name: "finance");

            migrationBuilder.EnsureSchema(
                name: "billing");

            migrationBuilder.EnsureSchema(
                name: "integration");

            migrationBuilder.EnsureSchema(
                name: "inventory");

            migrationBuilder.EnsureSchema(
                name: "crm");

            migrationBuilder.EnsureSchema(
                name: "core");

            migrationBuilder.CreateTable(
                name: "app_role",
                schema: "identity",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    normalized_name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    concurrency_stamp = table.Column<string>(type: "character varying(400)", maxLength: 400, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_app_role", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "tenant",
                schema: "core",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_tenant", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "role_claim",
                schema: "identity",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    role_id = table.Column<Guid>(type: "uuid", nullable: false),
                    claim_type = table.Column<string>(type: "character varying(400)", maxLength: 400, nullable: true),
                    claim_value = table.Column<string>(type: "character varying(400)", maxLength: 400, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_role_claim", x => x.id);
                    table.ForeignKey(
                        name: "fk_role_claim_app_role_role_id",
                        column: x => x.role_id,
                        principalSchema: "identity",
                        principalTable: "app_role",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "app_user",
                schema: "identity",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    full_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    max_discount_percent = table.Column<decimal>(type: "numeric(9,4)", precision: 9, scale: 4, nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    user_name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true, collation: "C"),
                    normalized_user_name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true, collation: "C"),
                    email = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true, collation: "C"),
                    normalized_email = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true, collation: "C"),
                    email_confirmed = table.Column<bool>(type: "boolean", nullable: false),
                    password_hash = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    security_stamp = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    concurrency_stamp = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    phone_number = table.Column<string>(type: "character varying(400)", maxLength: 400, nullable: true),
                    phone_number_confirmed = table.Column<bool>(type: "boolean", nullable: false),
                    two_factor_enabled = table.Column<bool>(type: "boolean", nullable: false),
                    lockout_end = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    lockout_enabled = table.Column<bool>(type: "boolean", nullable: false),
                    access_failed_count = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_app_user", x => x.id);
                    table.ForeignKey(
                        name: "fk_app_user_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalSchema: "core",
                        principalTable: "tenant",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "audit_event",
                schema: "audit",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    user_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    occurred_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    action = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false, collation: "C"),
                    target_type = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false, collation: "C"),
                    target_id = table.Column<Guid>(type: "uuid", nullable: true),
                    correlation_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    summary = table.Column<string>(type: "jsonb", maxLength: 400, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_audit_event", x => x.id);
                    table.ForeignKey(
                        name: "fk_audit_event_tenant_tenant_id",
                        column: x => x.tenant_id,
                        principalSchema: "core",
                        principalTable: "tenant",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "cash_account",
                schema: "finance",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    kind = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_cash_account", x => x.id);
                    table.ForeignKey(
                        name: "fk_cash_account_tenant_tenant_id",
                        column: x => x.tenant_id,
                        principalSchema: "core",
                        principalTable: "tenant",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "import_batch",
                schema: "integration",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    kind = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false, collation: "C"),
                    file_name = table.Column<string>(type: "character varying(260)", maxLength: 260, nullable: false),
                    file_sha256 = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false, collation: "C"),
                    sheet_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    mapping_json = table.Column<string>(type: "jsonb", maxLength: 400, nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    rows_read = table.Column<int>(type: "integer", nullable: false),
                    rows_valid = table.Column<int>(type: "integer", nullable: false),
                    rows_error = table.Column<int>(type: "integer", nullable: false),
                    applied_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    applied_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    reverted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    reverted_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_import_batch", x => x.id);
                    table.ForeignKey(
                        name: "fk_import_batch_tenant_tenant_id",
                        column: x => x.tenant_id,
                        principalSchema: "core",
                        principalTable: "tenant",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "item",
                schema: "inventory",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false, collation: "C"),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    kind = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    unit = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    unit_price = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    price_includes_vat = table.Column<bool>(type: "boolean", nullable: false),
                    vat_rate = table.Column<decimal>(type: "numeric(9,4)", precision: 9, scale: 4, nullable: false),
                    search_key = table.Column<string>(type: "character varying(400)", maxLength: 400, nullable: false, collation: "C"),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    import_batch_id = table.Column<Guid>(type: "uuid", nullable: true),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_item", x => x.id);
                    table.CheckConstraint("ck_item__price_nonnegative", "unit_price >= 0");
                    table.ForeignKey(
                        name: "fk_item_tenant_tenant_id",
                        column: x => x.tenant_id,
                        principalSchema: "core",
                        principalTable: "tenant",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "party",
                schema: "crm",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false, collation: "C"),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    kind = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    phone = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true, collation: "C"),
                    email = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true, collation: "C"),
                    tax_number = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true, collation: "C"),
                    notes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    search_key = table.Column<string>(type: "character varying(400)", maxLength: 400, nullable: false, collation: "C"),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    import_batch_id = table.Column<Guid>(type: "uuid", nullable: true),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_party", x => x.id);
                    table.ForeignKey(
                        name: "fk_party_tenant_tenant_id",
                        column: x => x.tenant_id,
                        principalSchema: "core",
                        principalTable: "tenant",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "user_claim",
                schema: "identity",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    claim_type = table.Column<string>(type: "character varying(400)", maxLength: 400, nullable: true),
                    claim_value = table.Column<string>(type: "character varying(400)", maxLength: 400, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_user_claim", x => x.id);
                    table.ForeignKey(
                        name: "fk_user_claim_app_user_user_id",
                        column: x => x.user_id,
                        principalSchema: "identity",
                        principalTable: "app_user",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "user_login",
                schema: "identity",
                columns: table => new
                {
                    login_provider = table.Column<string>(type: "character varying(400)", maxLength: 400, nullable: false),
                    provider_key = table.Column<string>(type: "character varying(400)", maxLength: 400, nullable: false),
                    provider_display_name = table.Column<string>(type: "character varying(400)", maxLength: 400, nullable: true),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_user_login", x => new { x.login_provider, x.provider_key });
                    table.ForeignKey(
                        name: "fk_user_login_app_user_user_id",
                        column: x => x.user_id,
                        principalSchema: "identity",
                        principalTable: "app_user",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "user_role",
                schema: "identity",
                columns: table => new
                {
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    role_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_user_role", x => new { x.user_id, x.role_id });
                    table.ForeignKey(
                        name: "fk_user_role_app_role_role_id",
                        column: x => x.role_id,
                        principalSchema: "identity",
                        principalTable: "app_role",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_user_role_app_user_user_id",
                        column: x => x.user_id,
                        principalSchema: "identity",
                        principalTable: "app_user",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "user_token",
                schema: "identity",
                columns: table => new
                {
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    login_provider = table.Column<string>(type: "character varying(400)", maxLength: 400, nullable: false),
                    name = table.Column<string>(type: "character varying(400)", maxLength: 400, nullable: false),
                    value = table.Column<string>(type: "character varying(400)", maxLength: 400, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_user_token", x => new { x.user_id, x.login_provider, x.name });
                    table.ForeignKey(
                        name: "fk_user_token_app_user_user_id",
                        column: x => x.user_id,
                        principalSchema: "identity",
                        principalTable: "app_user",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "import_row",
                schema: "integration",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    batch_id = table.Column<Guid>(type: "uuid", nullable: false),
                    row_number = table.Column<int>(type: "integer", nullable: false),
                    data_json = table.Column<string>(type: "jsonb", maxLength: 400, nullable: false),
                    is_valid = table.Column<bool>(type: "boolean", nullable: false),
                    errors = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    warnings = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    party_id = table.Column<Guid>(type: "uuid", nullable: true),
                    athlete_id = table.Column<Guid>(type: "uuid", nullable: true),
                    item_id = table.Column<Guid>(type: "uuid", nullable: true),
                    opening_balance_id = table.Column<Guid>(type: "uuid", nullable: true),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_import_row", x => x.id);
                    table.ForeignKey(
                        name: "fk_import_row_import_batch_batch_id",
                        column: x => x.batch_id,
                        principalSchema: "integration",
                        principalTable: "import_batch",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_import_row_tenant_tenant_id",
                        column: x => x.tenant_id,
                        principalSchema: "core",
                        principalTable: "tenant",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "stock_movement",
                schema: "inventory",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    item_id = table.Column<Guid>(type: "uuid", nullable: false),
                    quantity = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    source_type = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false, collation: "C"),
                    source_id = table.Column<Guid>(type: "uuid", nullable: false),
                    occurred_on = table.Column<DateOnly>(type: "date", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_stock_movement", x => x.id);
                    table.CheckConstraint("ck_stock_movement__quantity_nonzero", "quantity <> 0");
                    table.ForeignKey(
                        name: "fk_stock_movement_item_item_id",
                        column: x => x.item_id,
                        principalSchema: "inventory",
                        principalTable: "item",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_stock_movement_tenant_tenant_id",
                        column: x => x.tenant_id,
                        principalSchema: "core",
                        principalTable: "tenant",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "athlete",
                schema: "academy",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    party_id = table.Column<Guid>(type: "uuid", nullable: false),
                    first_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    last_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    group_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    birth_year = table.Column<int>(type: "integer", nullable: true),
                    search_key = table.Column<string>(type: "character varying(400)", maxLength: 400, nullable: false, collation: "C"),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    import_batch_id = table.Column<Guid>(type: "uuid", nullable: true),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_athlete", x => x.id);
                    table.ForeignKey(
                        name: "fk_athlete_party_party_id",
                        column: x => x.party_id,
                        principalSchema: "crm",
                        principalTable: "party",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_athlete_tenant_tenant_id",
                        column: x => x.tenant_id,
                        principalSchema: "core",
                        principalTable: "tenant",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "document",
                schema: "billing",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    party_id = table.Column<Guid>(type: "uuid", nullable: false),
                    direction = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    document_no = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false, collation: "C"),
                    document_date = table.Column<DateOnly>(type: "date", nullable: false),
                    net_total = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    vat_total = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    gross_total = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    note = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    cancel_reason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    cancelled_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    cancelled_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_document", x => x.id);
                    table.ForeignKey(
                        name: "fk_document_party_party_id",
                        column: x => x.party_id,
                        principalSchema: "crm",
                        principalTable: "party",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_document_tenant_tenant_id",
                        column: x => x.tenant_id,
                        principalSchema: "core",
                        principalTable: "tenant",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ledger_entry",
                schema: "finance",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    party_id = table.Column<Guid>(type: "uuid", nullable: false),
                    occurred_on = table.Column<DateOnly>(type: "date", nullable: false),
                    source_type = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false, collation: "C"),
                    source_id = table.Column<Guid>(type: "uuid", nullable: false),
                    debit = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    credit = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_ledger_entry", x => x.id);
                    table.CheckConstraint("ck_ledger_entry__one_side", "debit >= 0 AND credit >= 0 AND ((debit > 0) <> (credit > 0))");
                    table.CheckConstraint("ck_ledger_entry__source_type_allowed", "source_type IN ('sales_line', 'sales_line_reversal', 'purchase_line', 'purchase_line_reversal', 'payment', 'payment_reversal', 'opening_balance', 'approved_adjustment')");
                    table.ForeignKey(
                        name: "fk_ledger_entry_party_party_id",
                        column: x => x.party_id,
                        principalSchema: "crm",
                        principalTable: "party",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_ledger_entry_tenant_tenant_id",
                        column: x => x.tenant_id,
                        principalSchema: "core",
                        principalTable: "tenant",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "opening_balance",
                schema: "finance",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    party_id = table.Column<Guid>(type: "uuid", nullable: false),
                    amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    note = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    import_batch_id = table.Column<Guid>(type: "uuid", nullable: true),
                    decided_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    decided_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_opening_balance", x => x.id);
                    table.CheckConstraint("ck_opening_balance__amount_nonzero", "amount <> 0");
                    table.ForeignKey(
                        name: "fk_opening_balance_party_party_id",
                        column: x => x.party_id,
                        principalSchema: "crm",
                        principalTable: "party",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_opening_balance_tenant_tenant_id",
                        column: x => x.tenant_id,
                        principalSchema: "core",
                        principalTable: "tenant",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "payment",
                schema: "finance",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    party_id = table.Column<Guid>(type: "uuid", nullable: false),
                    direction = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    method = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    cash_account_id = table.Column<Guid>(type: "uuid", nullable: false),
                    amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    occurred_on = table.Column<DateOnly>(type: "date", nullable: false),
                    reference = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    reverses_payment_id = table.Column<Guid>(type: "uuid", nullable: true),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_payment", x => x.id);
                    table.CheckConstraint("ck_payment__amount_positive", "amount > 0");
                    table.ForeignKey(
                        name: "fk_payment_cash_account_cash_account_id",
                        column: x => x.cash_account_id,
                        principalSchema: "finance",
                        principalTable: "cash_account",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_payment_party_party_id",
                        column: x => x.party_id,
                        principalSchema: "crm",
                        principalTable: "party",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_payment_payment_reverses_payment_id",
                        column: x => x.reverses_payment_id,
                        principalSchema: "finance",
                        principalTable: "payment",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_payment_tenant_tenant_id",
                        column: x => x.tenant_id,
                        principalSchema: "core",
                        principalTable: "tenant",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "trade_line",
                schema: "billing",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    party_id = table.Column<Guid>(type: "uuid", nullable: false),
                    item_id = table.Column<Guid>(type: "uuid", nullable: false),
                    direction = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    occurred_on = table.Column<DateOnly>(type: "date", nullable: false),
                    quantity = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    unit_price = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    price_includes_vat = table.Column<bool>(type: "boolean", nullable: false),
                    discount_percent = table.Column<decimal>(type: "numeric(9,4)", precision: 9, scale: 4, nullable: false),
                    vat_rate = table.Column<decimal>(type: "numeric(9,4)", precision: 9, scale: 4, nullable: false),
                    net_amount = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: false),
                    vat_amount = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: false),
                    gross_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    documented_quantity = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    reverses_line_id = table.Column<Guid>(type: "uuid", nullable: true),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_trade_line", x => x.id);
                    table.CheckConstraint("ck_trade_line__discount_range", "discount_percent >= 0 AND discount_percent <= 100");
                    table.CheckConstraint("ck_trade_line__documented_within_quantity", "abs(documented_quantity) <= abs(quantity) AND (documented_quantity = 0 OR sign(documented_quantity) = sign(quantity))");
                    table.CheckConstraint("ck_trade_line__quantity_nonzero", "quantity <> 0");
                    table.CheckConstraint("ck_trade_line__reversal_is_negative", "(reverses_line_id IS NULL AND quantity > 0) OR (reverses_line_id IS NOT NULL AND quantity < 0)");
                    table.ForeignKey(
                        name: "fk_trade_line_item_item_id",
                        column: x => x.item_id,
                        principalSchema: "inventory",
                        principalTable: "item",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_trade_line_party_party_id",
                        column: x => x.party_id,
                        principalSchema: "crm",
                        principalTable: "party",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_trade_line_tenant_tenant_id",
                        column: x => x.tenant_id,
                        principalSchema: "core",
                        principalTable: "tenant",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_trade_line_trade_line_reverses_line_id",
                        column: x => x.reverses_line_id,
                        principalSchema: "billing",
                        principalTable: "trade_line",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "document_vat_line",
                schema: "billing",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    document_id = table.Column<Guid>(type: "uuid", nullable: false),
                    vat_rate = table.Column<decimal>(type: "numeric(9,4)", precision: 9, scale: 4, nullable: false),
                    net_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    vat_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_document_vat_line", x => x.id);
                    table.ForeignKey(
                        name: "fk_document_vat_line_document_document_id",
                        column: x => x.document_id,
                        principalSchema: "billing",
                        principalTable: "document",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_document_vat_line_tenant_tenant_id",
                        column: x => x.tenant_id,
                        principalSchema: "core",
                        principalTable: "tenant",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "document_link",
                schema: "billing",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    document_id = table.Column<Guid>(type: "uuid", nullable: false),
                    trade_line_id = table.Column<Guid>(type: "uuid", nullable: false),
                    quantity = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_document_link", x => x.id);
                    table.CheckConstraint("ck_document_link__quantity_nonzero", "quantity <> 0");
                    table.ForeignKey(
                        name: "fk_document_link_document_document_id",
                        column: x => x.document_id,
                        principalSchema: "billing",
                        principalTable: "document",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_document_link_tenant_tenant_id",
                        column: x => x.tenant_id,
                        principalSchema: "core",
                        principalTable: "tenant",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_document_link_trade_line_trade_line_id",
                        column: x => x.trade_line_id,
                        principalSchema: "billing",
                        principalTable: "trade_line",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "payment_allocation",
                schema: "finance",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    payment_id = table.Column<Guid>(type: "uuid", nullable: false),
                    trade_line_id = table.Column<Guid>(type: "uuid", nullable: false),
                    amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    reverses_allocation_id = table.Column<Guid>(type: "uuid", nullable: true),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_payment_allocation", x => x.id);
                    table.CheckConstraint("ck_payment_allocation__amount_nonzero", "amount <> 0");
                    table.ForeignKey(
                        name: "fk_payment_allocation_payment_payment_id",
                        column: x => x.payment_id,
                        principalSchema: "finance",
                        principalTable: "payment",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_payment_allocation_tenant_tenant_id",
                        column: x => x.tenant_id,
                        principalSchema: "core",
                        principalTable: "tenant",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_payment_allocation_trade_line_trade_line_id",
                        column: x => x.trade_line_id,
                        principalSchema: "billing",
                        principalTable: "trade_line",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "RoleNameIndex",
                schema: "identity",
                table: "app_role",
                column: "normalized_name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "EmailIndex",
                schema: "identity",
                table: "app_user",
                column: "normalized_email");

            migrationBuilder.CreateIndex(
                name: "ix_app_user_tenant_id",
                schema: "identity",
                table: "app_user",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "UserNameIndex",
                schema: "identity",
                table: "app_user",
                column: "normalized_user_name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_athlete_party_id",
                schema: "academy",
                table: "athlete",
                column: "party_id");

            migrationBuilder.CreateIndex(
                name: "ix_athlete_tenant_id",
                schema: "academy",
                table: "athlete",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "ix_audit_event_tenant_id_occurred_at",
                schema: "audit",
                table: "audit_event",
                columns: new[] { "tenant_id", "occurred_at" });

            migrationBuilder.CreateIndex(
                name: "ix_cash_account_tenant_id",
                schema: "finance",
                table: "cash_account",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "ix_document_party_id",
                schema: "billing",
                table: "document",
                column: "party_id");

            migrationBuilder.CreateIndex(
                name: "uq_document__tenant_party_purchase_no",
                schema: "billing",
                table: "document",
                columns: new[] { "tenant_id", "party_id", "document_no" },
                unique: true,
                filter: "direction = 'purchase' AND status = 'issued'");

            migrationBuilder.CreateIndex(
                name: "uq_document__tenant_sale_no",
                schema: "billing",
                table: "document",
                columns: new[] { "tenant_id", "document_no" },
                unique: true,
                filter: "direction = 'sale' AND status = 'issued'");

            migrationBuilder.CreateIndex(
                name: "ix_document_link_document_id",
                schema: "billing",
                table: "document_link",
                column: "document_id");

            migrationBuilder.CreateIndex(
                name: "ix_document_link_tenant_id",
                schema: "billing",
                table: "document_link",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "ix_document_link_trade_line_id",
                schema: "billing",
                table: "document_link",
                column: "trade_line_id");

            migrationBuilder.CreateIndex(
                name: "ix_document_vat_line_document_id",
                schema: "billing",
                table: "document_vat_line",
                column: "document_id");

            migrationBuilder.CreateIndex(
                name: "ix_document_vat_line_tenant_id",
                schema: "billing",
                table: "document_vat_line",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "uq_import_batch__tenant_kind_hash_applied",
                schema: "integration",
                table: "import_batch",
                columns: new[] { "tenant_id", "kind", "file_sha256" },
                unique: true,
                filter: "status = 'applied'");

            migrationBuilder.CreateIndex(
                name: "ix_import_row_batch_id_row_number",
                schema: "integration",
                table: "import_row",
                columns: new[] { "batch_id", "row_number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_import_row_tenant_id",
                schema: "integration",
                table: "import_row",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "uq_item__tenant_code",
                schema: "inventory",
                table: "item",
                columns: new[] { "tenant_id", "code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_ledger_entry__tenant_party_occurred_on",
                schema: "finance",
                table: "ledger_entry",
                columns: new[] { "tenant_id", "party_id", "occurred_on" });

            migrationBuilder.CreateIndex(
                name: "ix_ledger_entry_party_id",
                schema: "finance",
                table: "ledger_entry",
                column: "party_id");

            migrationBuilder.CreateIndex(
                name: "uq_ledger_entry__tenant_source",
                schema: "finance",
                table: "ledger_entry",
                columns: new[] { "tenant_id", "source_type", "source_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_opening_balance_party_id",
                schema: "finance",
                table: "opening_balance",
                column: "party_id");

            migrationBuilder.CreateIndex(
                name: "ix_opening_balance_tenant_id",
                schema: "finance",
                table: "opening_balance",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "ix_party__tenant_phone",
                schema: "crm",
                table: "party",
                columns: new[] { "tenant_id", "phone" });

            migrationBuilder.CreateIndex(
                name: "uq_party__tenant_code",
                schema: "crm",
                table: "party",
                columns: new[] { "tenant_id", "code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_payment_cash_account_id",
                schema: "finance",
                table: "payment",
                column: "cash_account_id");

            migrationBuilder.CreateIndex(
                name: "ix_payment_party_id",
                schema: "finance",
                table: "payment",
                column: "party_id");

            migrationBuilder.CreateIndex(
                name: "ix_payment_tenant_id",
                schema: "finance",
                table: "payment",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "uq_payment__reverses_payment",
                schema: "finance",
                table: "payment",
                column: "reverses_payment_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_payment_allocation_payment_id",
                schema: "finance",
                table: "payment_allocation",
                column: "payment_id");

            migrationBuilder.CreateIndex(
                name: "ix_payment_allocation_tenant_id",
                schema: "finance",
                table: "payment_allocation",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "ix_payment_allocation_trade_line_id",
                schema: "finance",
                table: "payment_allocation",
                column: "trade_line_id");

            migrationBuilder.CreateIndex(
                name: "uq_payment_allocation__reverses",
                schema: "finance",
                table: "payment_allocation",
                column: "reverses_allocation_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_role_claim_role_id",
                schema: "identity",
                table: "role_claim",
                column: "role_id");

            migrationBuilder.CreateIndex(
                name: "ix_stock_movement_item_id",
                schema: "inventory",
                table: "stock_movement",
                column: "item_id");

            migrationBuilder.CreateIndex(
                name: "uq_stock_movement__tenant_source",
                schema: "inventory",
                table: "stock_movement",
                columns: new[] { "tenant_id", "source_type", "source_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_trade_line_item_id",
                schema: "billing",
                table: "trade_line",
                column: "item_id");

            migrationBuilder.CreateIndex(
                name: "ix_trade_line_party_id",
                schema: "billing",
                table: "trade_line",
                column: "party_id");

            migrationBuilder.CreateIndex(
                name: "ix_trade_line_reverses_line_id",
                schema: "billing",
                table: "trade_line",
                column: "reverses_line_id");

            migrationBuilder.CreateIndex(
                name: "ix_trade_line_tenant_id_party_id_occurred_on",
                schema: "billing",
                table: "trade_line",
                columns: new[] { "tenant_id", "party_id", "occurred_on" });

            migrationBuilder.CreateIndex(
                name: "ix_user_claim_user_id",
                schema: "identity",
                table: "user_claim",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_user_login_user_id",
                schema: "identity",
                table: "user_login",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_user_role_role_id",
                schema: "identity",
                table: "user_role",
                column: "role_id");

            // Guvenlik standardi bolum 4: rol yetkileri ve RLS politikalari.
            MigrationSecurity.Apply(migrationBuilder);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "athlete",
                schema: "academy");

            migrationBuilder.DropTable(
                name: "audit_event",
                schema: "audit");

            migrationBuilder.DropTable(
                name: "document_link",
                schema: "billing");

            migrationBuilder.DropTable(
                name: "document_vat_line",
                schema: "billing");

            migrationBuilder.DropTable(
                name: "import_row",
                schema: "integration");

            migrationBuilder.DropTable(
                name: "ledger_entry",
                schema: "finance");

            migrationBuilder.DropTable(
                name: "opening_balance",
                schema: "finance");

            migrationBuilder.DropTable(
                name: "payment_allocation",
                schema: "finance");

            migrationBuilder.DropTable(
                name: "role_claim",
                schema: "identity");

            migrationBuilder.DropTable(
                name: "stock_movement",
                schema: "inventory");

            migrationBuilder.DropTable(
                name: "user_claim",
                schema: "identity");

            migrationBuilder.DropTable(
                name: "user_login",
                schema: "identity");

            migrationBuilder.DropTable(
                name: "user_role",
                schema: "identity");

            migrationBuilder.DropTable(
                name: "user_token",
                schema: "identity");

            migrationBuilder.DropTable(
                name: "document",
                schema: "billing");

            migrationBuilder.DropTable(
                name: "import_batch",
                schema: "integration");

            migrationBuilder.DropTable(
                name: "payment",
                schema: "finance");

            migrationBuilder.DropTable(
                name: "trade_line",
                schema: "billing");

            migrationBuilder.DropTable(
                name: "app_role",
                schema: "identity");

            migrationBuilder.DropTable(
                name: "app_user",
                schema: "identity");

            migrationBuilder.DropTable(
                name: "cash_account",
                schema: "finance");

            migrationBuilder.DropTable(
                name: "item",
                schema: "inventory");

            migrationBuilder.DropTable(
                name: "party",
                schema: "crm");

            migrationBuilder.DropTable(
                name: "tenant",
                schema: "core");
        }
    }
}
