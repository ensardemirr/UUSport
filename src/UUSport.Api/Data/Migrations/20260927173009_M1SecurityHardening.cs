using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace UUSport.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class M1SecurityHardening : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "value",
                schema: "identity",
                table: "user_token",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(400)",
                oldMaxLength: 400,
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ip_address",
                schema: "audit",
                table: "audit_event",
                type: "character varying(400)",
                maxLength: 400,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ip_address",
                schema: "audit",
                table: "audit_event");

            migrationBuilder.AlterColumn<string>(
                name: "value",
                schema: "identity",
                table: "user_token",
                type: "character varying(400)",
                maxLength: 400,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(2000)",
                oldMaxLength: 2000,
                oldNullable: true);
        }
    }
}
