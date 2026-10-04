using Microsoft.EntityFrameworkCore;
using Npgsql;
using UUSport.Api.Data;
using UUSport.Api.Infrastructure;

namespace UUSport.Tests;

/// <summary>
/// Guvenlik standardi bolum 4'un katalogdan dogrulanmasi. Iki yol sinanir: mevcut test veritabani
/// (yukseltme) ve her kosuda sifirdan kurulan gecici veritabani (temiz kurulum). 26.09 hatasi yalniz
/// temiz kurulumda gorunuyordu; bu yuzden ikinci yol zorunludur.
/// </summary>
public sealed class SchemaGuardTests
{
    private static readonly DatabaseOptions Options = new();

    [Fact]
    public Task YukseltmeYolu_TestVeritabani() => AssertInvariantsAsync("uusport_test");

    [Fact]
    public async Task TemizKurulum_SifirVeritabani()
    {
        var name = "uusport_guard_" + Guid.NewGuid().ToString("N")[..12];
        var server = Options.BuildConnectionString(asOwner: true, "postgres");
        await ExecAsync(server,
            $"CREATE DATABASE {name} TEMPLATE template0 ENCODING 'UTF8' LOCALE_PROVIDER icu ICU_LOCALE 'tr-TR' LOCALE 'C'");
        try
        {
            var conn = Options.BuildConnectionString(asOwner: true, name);
            await ExecAsync(conn,
                $"REVOKE ALL ON DATABASE {name} FROM PUBLIC; " +
                $"GRANT CONNECT, TEMPORARY ON DATABASE {name} TO {MigrationSecurity.AppRole}; " +
                "REVOKE CREATE ON SCHEMA public FROM PUBLIC;");
            await using (var db = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>()
                .UseNpgsql(conn).UseSnakeCaseNamingConvention().Options))
            {
                await db.Database.MigrateAsync();
            }
            await AssertInvariantsAsync(name);
        }
        finally
        {
            NpgsqlConnection.ClearAllPools();
            await ExecAsync(server, $"DROP DATABASE IF EXISTS {name} WITH (FORCE)");
        }
    }

    private static async Task AssertInvariantsAsync(string database)
    {
        await using var conn = new NpgsqlConnection(Options.BuildConnectionString(asOwner: true, database));
        await conn.OpenAsync();

        // 1) tenant_id tasiyan her tabloda RLS acik, zorlanmis ve kiraci politikasi var.
        //    identity semasi bilincli istisna (SAAS-02: kiraci filtresi uygulama katmaninda, SaaS oncesi ele alinacak).
        var rls = await QueryAsync(conn, """
            SELECT n.nspname || '.' || c.relname, c.relrowsecurity, c.relforcerowsecurity,
                   EXISTS (SELECT 1 FROM pg_policies p
                           WHERE p.schemaname = n.nspname AND p.tablename = c.relname AND p.policyname = 'tenant_isolation')
            FROM pg_class c JOIN pg_namespace n ON n.oid = c.relnamespace
            WHERE c.relkind = 'r'
              AND n.nspname NOT IN ('pg_catalog', 'information_schema', 'identity', 'public')
              AND EXISTS (SELECT 1 FROM pg_attribute a
                          WHERE a.attrelid = c.oid AND a.attname = 'tenant_id' AND NOT a.attisdropped)
            ORDER BY 1
            """);
        Assert.True(rls.Count >= 19, $"{database}: beklenenden az kiraci tablosu ({rls.Count})");
        var rlsViolations = rls.Where(r => !(bool)r[1] || !(bool)r[2] || !(bool)r[3]).Select(r => (string)r[0]).ToList();
        Assert.True(rlsViolations.Count == 0, $"{database}: RLS/FORCE/politika eksik: {string.Join(", ", rlsViolations)}");

        // 2) Degistirilemez tablolarda uygulama rolunun UPDATE/DELETE/TRUNCATE yetkisi yok.
        var appendOnlyViolations = new List<string>();
        foreach (var table in MigrationSecurity.AppendOnlyTablesAsOf20260927)
            foreach (var privilege in new[] { "UPDATE", "DELETE", "TRUNCATE" })
            {
                var rows = await QueryAsync(conn, $"SELECT has_table_privilege('{MigrationSecurity.AppRole}', '{table}', '{privilege}')");
                if ((bool)rows[0][0]) appendOnlyViolations.Add($"{table}:{privilege}");
            }
        Assert.True(appendOnlyViolations.Count == 0, $"{database}: append-only ihlali: {string.Join(", ", appendOnlyViolations)}");

        // 3) public semasinda uygulama tablosu yok.
        var publicTables = await QueryAsync(conn, """
            SELECT c.relname FROM pg_class c JOIN pg_namespace n ON n.oid = c.relnamespace
            WHERE n.nspname = 'public' AND c.relkind = 'r' AND c.relname <> '__EFMigrationsHistory'
            """);
        Assert.Empty(publicTables);

        // 4) Uygulama rolu RLS'i asamaz.
        var role = await QueryAsync(conn,
            $"SELECT rolsuper, rolbypassrls FROM pg_roles WHERE rolname = '{MigrationSecurity.AppRole}'");
        Assert.False((bool)role[0][0], "uygulama rolu superuser olmamali");
        Assert.False((bool)role[0][1], "uygulama rolu BYPASSRLS olmamali");
    }

    private static async Task ExecAsync(string connectionString, string sql)
    {
        await using var conn = new NpgsqlConnection(connectionString);
        await conn.OpenAsync();
        await using var cmd = new NpgsqlCommand(sql, conn);
        await cmd.ExecuteNonQueryAsync();
    }

    private static async Task<List<object[]>> QueryAsync(NpgsqlConnection conn, string sql)
    {
        await using var cmd = new NpgsqlCommand(sql, conn);
        await using var reader = await cmd.ExecuteReaderAsync();
        var rows = new List<object[]>();
        while (await reader.ReadAsync())
        {
            var values = new object[reader.FieldCount];
            reader.GetValues(values);
            rows.Add(values);
        }
        return rows;
    }
}
