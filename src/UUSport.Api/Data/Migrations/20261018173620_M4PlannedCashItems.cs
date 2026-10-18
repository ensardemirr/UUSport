using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace UUSport.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class M4PlannedCashItems : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<short>(
                name: "payment_due_day",
                schema: "finance",
                table: "cash_account",
                type: "smallint",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "planned_cash_item",
                schema: "finance",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    kind = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    day_of_month = table.Column<short>(type: "smallint", nullable: false),
                    interval_months = table.Column<short>(type: "smallint", nullable: false),
                    start_month = table.Column<DateOnly>(type: "date", nullable: false),
                    end_month = table.Column<DateOnly>(type: "date", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_planned_cash_item", x => x.id);
                    table.CheckConstraint("ck_planned_cash_item__amount_positive", "amount > 0");
                    table.CheckConstraint("ck_planned_cash_item__day_range", "day_of_month BETWEEN 1 AND 28");
                    table.CheckConstraint("ck_planned_cash_item__interval_allowed", "interval_months IN (1, 2, 3, 6, 12)");
                    table.CheckConstraint("ck_planned_cash_item__month_order", "end_month IS NULL OR end_month >= start_month");
                    table.ForeignKey(
                        name: "fk_planned_cash_item_tenant_tenant_id",
                        column: x => x.tenant_id,
                        principalSchema: "core",
                        principalTable: "tenant",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.AddCheckConstraint(
                name: "ck_cash_account__payment_due_day",
                schema: "finance",
                table: "cash_account",
                sql: "payment_due_day IS NULL OR payment_due_day BETWEEN 1 AND 31");

            migrationBuilder.CreateIndex(
                name: "ix_planned_cash_item__tenant_active",
                schema: "finance",
                table: "planned_cash_item",
                columns: new[] { "tenant_id", "is_active" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "planned_cash_item",
                schema: "finance");

            migrationBuilder.DropCheckConstraint(
                name: "ck_cash_account__payment_due_day",
                schema: "finance",
                table: "cash_account");

            migrationBuilder.DropColumn(
                name: "payment_due_day",
                schema: "finance",
                table: "cash_account");
        }
    }
}
