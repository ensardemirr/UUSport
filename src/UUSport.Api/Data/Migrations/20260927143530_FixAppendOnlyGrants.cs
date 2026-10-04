using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace UUSport.Api.Data.Migrations
{
    /// <summary>
    /// 20260926062253'teki ApplySchemaGrants cagrisi, sifirdan kurulan veritabaninda append-only
    /// tablolara UPDATE yetkisini geri veriyordu. Bu migration korumayi onarir; mevcut dogru
    /// veritabanlarinda etkisizdir. Down bilerek bostur: yetki geri verilmez.
    /// </summary>
    public partial class FixAppendOnlyGrants : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            MigrationSecurity.RevokeUpdateDelete(migrationBuilder, MigrationSecurity.AppendOnlyTablesAsOf20260927);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
        }
    }
}
