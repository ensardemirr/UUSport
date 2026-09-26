using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace UUSport.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddLoansChecksAndAccountCodes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "account_code",
                schema: "crm",
                table: "party",
                type: "character varying(40)",
                maxLength: 40,
                nullable: true,
                collation: "C");

            migrationBuilder.AddColumn<string>(
                name: "account_code",
                schema: "inventory",
                table: "item",
                type: "character varying(40)",
                maxLength: 40,
                nullable: true,
                collation: "C");

            migrationBuilder.AddColumn<Guid>(
                name: "parent_account_id",
                schema: "finance",
                table: "cash_account",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "check_note",
                schema: "finance",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    party_id = table.Column<Guid>(type: "uuid", nullable: false),
                    direction = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    check_no = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false, collation: "C"),
                    bank_name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    due_date = table.Column<DateOnly>(type: "date", nullable: false),
                    amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    batch_id = table.Column<Guid>(type: "uuid", nullable: false),
                    payment_id = table.Column<Guid>(type: "uuid", nullable: true),
                    status_note = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_check_note", x => x.id);
                    table.CheckConstraint("ck_check_note__amount_positive", "amount > 0");
                    table.ForeignKey(
                        name: "fk_check_note_party_party_id",
                        column: x => x.party_id,
                        principalSchema: "crm",
                        principalTable: "party",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_check_note_payments_payment_id",
                        column: x => x.payment_id,
                        principalSchema: "finance",
                        principalTable: "payment",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_check_note_tenant_tenant_id",
                        column: x => x.tenant_id,
                        principalSchema: "core",
                        principalTable: "tenant",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "loan",
                schema: "finance",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    party_id = table.Column<Guid>(type: "uuid", nullable: false),
                    cash_account_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    principal_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    start_date = table.Column<DateOnly>(type: "date", nullable: false),
                    notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_loan", x => x.id);
                    table.CheckConstraint("ck_loan__principal_positive", "principal_amount > 0");
                    table.ForeignKey(
                        name: "fk_loan_cash_account_cash_account_id",
                        column: x => x.cash_account_id,
                        principalSchema: "finance",
                        principalTable: "cash_account",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_loan_party_party_id",
                        column: x => x.party_id,
                        principalSchema: "crm",
                        principalTable: "party",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_loan_tenant_tenant_id",
                        column: x => x.tenant_id,
                        principalSchema: "core",
                        principalTable: "tenant",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "loan_installment",
                schema: "finance",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    loan_id = table.Column<Guid>(type: "uuid", nullable: false),
                    installment_no = table.Column<int>(type: "integer", nullable: false),
                    due_date = table.Column<DateOnly>(type: "date", nullable: false),
                    principal_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    interest_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    fee_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    payment_id = table.Column<Guid>(type: "uuid", nullable: true),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_loan_installment", x => x.id);
                    table.CheckConstraint("ck_loan_installment__amounts_nonnegative", "principal_amount >= 0 AND interest_amount >= 0 AND fee_amount >= 0");
                    table.CheckConstraint("ck_loan_installment__no_positive", "installment_no > 0");
                    table.ForeignKey(
                        name: "fk_loan_installment_loan_loan_id",
                        column: x => x.loan_id,
                        principalSchema: "finance",
                        principalTable: "loan",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_loan_installment_payments_payment_id",
                        column: x => x.payment_id,
                        principalSchema: "finance",
                        principalTable: "payment",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_loan_installment_tenant_tenant_id",
                        column: x => x.tenant_id,
                        principalSchema: "core",
                        principalTable: "tenant",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_cash_account_parent_account_id",
                schema: "finance",
                table: "cash_account",
                column: "parent_account_id");

            migrationBuilder.CreateIndex(
                name: "ix_check_note__tenant_batch",
                schema: "finance",
                table: "check_note",
                columns: new[] { "tenant_id", "batch_id" });

            migrationBuilder.CreateIndex(
                name: "ix_check_note_party_id",
                schema: "finance",
                table: "check_note",
                column: "party_id");

            migrationBuilder.CreateIndex(
                name: "ix_check_note_payment_id",
                schema: "finance",
                table: "check_note",
                column: "payment_id");

            migrationBuilder.CreateIndex(
                name: "ix_loan_cash_account_id",
                schema: "finance",
                table: "loan",
                column: "cash_account_id");

            migrationBuilder.CreateIndex(
                name: "ix_loan_party_id",
                schema: "finance",
                table: "loan",
                column: "party_id");

            migrationBuilder.CreateIndex(
                name: "ix_loan_tenant_id",
                schema: "finance",
                table: "loan",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "ix_loan_installment_payment_id",
                schema: "finance",
                table: "loan_installment",
                column: "payment_id");

            migrationBuilder.CreateIndex(
                name: "ix_loan_installment_tenant_id",
                schema: "finance",
                table: "loan_installment",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "uq_loan_installment__loan_no",
                schema: "finance",
                table: "loan_installment",
                columns: new[] { "loan_id", "installment_no" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "fk_cash_account_cash_account_parent_account_id",
                schema: "finance",
                table: "cash_account",
                column: "parent_account_id",
                principalSchema: "finance",
                principalTable: "cash_account",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            // Guvenlik standardi bolum 4: yalniz bu migration'in ekledigi tablolar icin.
            // loan/loan_installment/check_note degistirilebilir (Document deseni); append-only degil.
            MigrationSecurity.ApplySchemaGrants(migrationBuilder);
            MigrationSecurity.ApplyRls(migrationBuilder, "finance.loan", "finance.loan_installment", "finance.check_note");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_cash_account_cash_account_parent_account_id",
                schema: "finance",
                table: "cash_account");

            migrationBuilder.DropTable(
                name: "check_note",
                schema: "finance");

            migrationBuilder.DropTable(
                name: "loan_installment",
                schema: "finance");

            migrationBuilder.DropTable(
                name: "loan",
                schema: "finance");

            migrationBuilder.DropIndex(
                name: "ix_cash_account_parent_account_id",
                schema: "finance",
                table: "cash_account");

            migrationBuilder.DropColumn(
                name: "account_code",
                schema: "crm",
                table: "party");

            migrationBuilder.DropColumn(
                name: "account_code",
                schema: "inventory",
                table: "item");

            migrationBuilder.DropColumn(
                name: "parent_account_id",
                schema: "finance",
                table: "cash_account");
        }
    }
}
