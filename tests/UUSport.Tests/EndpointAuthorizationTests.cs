using System.Net;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.Metadata;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Routing.Patterns;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using UUSport.Api.Data;
using UUSport.Api.Features.Auth;
using UUSport.Api.Infrastructure;

namespace UUSport.Tests;

/// <summary>
/// Uc yetki denetimi (guvenlik standardi S-14): sistemdeki gercek HTTP hattinda TUM /api uclari
/// (EndpointDataSource'tan otomatik kesfedilir, elle tutulan bir liste degildir) taranir.
/// Oturumsuz istek yetkisi olan hicbir uca ulasamamali; Muhasebe rolu yalniz kendi yetkisindeki
/// uclara ulasabilmeli, yonetici-ozel uclarda 403 almali. Bu, tam olarak 27.09 P0 hatasinin
/// (bir korumanin sifir/yeni ortamda sessizce acik kalmasi) HTTP katmanindaki karsiligini yakalar.
/// </summary>
[Collection("WebHost")]
public sealed class EndpointAuthorizationTests : IAsyncLifetime
{
    private static readonly DatabaseOptions Options = new();
    private string _dbName = null!;
    private WebApplicationFactory<Program> _factory = null!;

    private const string AdminUser = "test-admin";
    private const string StaffUser = "test-muhasebe";
    private const string Password = "SifreTest12345678";

    /// <summary>Anonim erisimi bilerek acik olan tek uclar. Yeni bir uc buraya eklenmeden anonim
    /// birakilirsa asagidaki test onu "YETKI TANIMSIZ" olarak isaretler.</summary>
    private static readonly HashSet<(string Method, string Path)> AnonymousAllowed = new()
    {
        ("GET", "/api/setup/status"), ("POST", "/api/setup"),
        ("POST", "/api/auth/login"), ("POST", "/api/auth/login/mfa"), ("POST", "/api/auth/login/mfa/recovery"),
        ("POST", "/api/auth/logout"), ("GET", "/api/auth/me"), ("GET", "/api/health"),
    };

    /// <summary>
    /// Yonetici-ozel olmasi GEREKEN uclarin normatif listesi (bir katalog, calisan koddan degil is
    /// niyetinden turer). Kod bu listeyle her iki yonde de karsilastirilir: biri Admin oldugunu
    /// soylerken digeri soylemiyorsa test kirmizi olur - boylece "RequireAuthorization(Admin)
    /// yanlislikla silindi" veya "gereksiz yere Admin yapildi" sessizce gecmez. Yeni bir
    /// yonetici-ozel uc eklendiginde buraya eklenmelidir (append-only tablo katalogu ile ayni disiplin).
    /// </summary>
    private static readonly HashSet<(string Method, string Path)> AdminOnlyCatalog = new()
    {
        ("POST", "/api/loans"),
        ("POST", "/api/cash-accounts"), ("PUT", "/api/cash-accounts/{id:guid}"),
        ("GET", "/api/users"), ("POST", "/api/users"), ("PUT", "/api/users/{id:guid}"),
        ("POST", "/api/users/{id:guid}/password"), ("POST", "/api/users/{id:guid}/reset-mfa"),
        ("GET", "/api/users/permission-keys"),
        ("GET", "/api/audit"),
        // Grup yoneticisi (ADR-012): sirket acma/duzenleme yalniz patrona aittir; Muhasebe 403 alir.
        ("GET", "/api/companies/all"), ("POST", "/api/companies"), ("PUT", "/api/companies/{id:guid}"),
    };

    public async Task InitializeAsync()
    {
        _dbName = "uusport_authtest_" + Guid.NewGuid().ToString("N")[..12];
        var server = Options.BuildConnectionString(asOwner: true, "postgres");
        await ExecAsync(server,
            $"CREATE DATABASE {_dbName} TEMPLATE template0 ENCODING 'UTF8' LOCALE_PROVIDER icu ICU_LOCALE 'tr-TR' LOCALE 'C'");
        var conn = Options.BuildConnectionString(asOwner: true, _dbName);
        await ExecAsync(conn,
            $"REVOKE ALL ON DATABASE {_dbName} FROM PUBLIC; " +
            $"GRANT CONNECT, TEMPORARY ON DATABASE {_dbName} TO {MigrationSecurity.AppRole}; " +
            "REVOKE CREATE ON SCHEMA public FROM PUBLIC;");
        await using (var db = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(conn).UseSnakeCaseNamingConvention().Options))
        {
            await db.Database.MigrateAsync();
        }

        Environment.SetEnvironmentVariable("Database__Database", _dbName);
        try
        {
            _factory = new WebApplicationFactory<Program>();
            _ = _factory.Server; // env degiskeni okunurken host'u simdi kur
        }
        finally
        {
            Environment.SetEnvironmentVariable("Database__Database", null);
        }
    }

    public async Task DisposeAsync()
    {
        await _factory.DisposeAsync();
        NpgsqlConnection.ClearAllPools();
        var server = Options.BuildConnectionString(asOwner: true, "postgres");
        await ExecAsync(server, $"DROP DATABASE IF EXISTS {_dbName} WITH (FORCE)");
    }

    [Fact]
    public async Task TumUclarYetkiDenetiminden_Gecer()
    {
        using var adminClient = await SetupAdminAsync();
        using var staffClient = await CreateStaffSessionAsync(adminClient);
        using var anonClient = _factory.CreateClient();

        var endpoints = _factory.Services.GetRequiredService<EndpointDataSource>().Endpoints
            .OfType<RouteEndpoint>()
            .Where(e => (e.RoutePattern.RawText ?? "").StartsWith("/api", StringComparison.Ordinal))
            .ToList();
        Assert.True(endpoints.Count > 40, $"Beklenenden az uc bulundu ({endpoints.Count}); kesif calismiyor olabilir.");

        var failures = new List<string>();

        foreach (var ep in endpoints)
        {
            var path = ResolvePath(ep.RoutePattern);
            var rawText = (ep.RoutePattern.RawText ?? "/").TrimEnd('/');
            if (rawText.Length == 0) rawText = "/";
            var isMultipart = ep.Metadata.GetMetadata<IAcceptsMetadata>()?.ContentTypes
                .Any(c => c.Contains("multipart/form-data", StringComparison.OrdinalIgnoreCase)) == true;
            var methods = ep.Metadata.GetMetadata<HttpMethodMetadata>()?.HttpMethods ?? ["GET"];
            var authData = ep.Metadata.GetOrderedMetadata<IAuthorizeData>();
            var requiresAuth = authData.Count > 0;
            var allowsAnonymous = ep.Metadata.GetMetadata<IAllowAnonymous>() is not null;
            // Grup ve uc-ozel RequireAuthorization birleşik (VE) uygulanir; herhangi biri Admin
            // ise personel (Muhasebe) bu uca giremez.
            var isAdminOnly = authData.Any(a => a.Policy is Policies.Admin or Policies.GroupAdmin);

            foreach (var method in methods)
            {
                var expectedAnonymous = AnonymousAllowed.Contains((method, rawText));

                if (!requiresAuth && !allowsAnonymous && !expectedAnonymous)
                {
                    failures.Add($"YETKI TANIMSIZ: {method} {rawText} (RequireAuthorization eksik olabilir)");
                    continue;
                }
                if (expectedAnonymous) continue;

                var resp = await SendAsync(anonClient, method, path, isMultipart: isMultipart);
                if (resp.StatusCode != HttpStatusCode.Unauthorized)
                    failures.Add($"401 BEKLENDI (oturumsuz): {method} {path} -> {(int)resp.StatusCode}");

                var catalogSaysAdmin = AdminOnlyCatalog.Contains((method, rawText));
                if (catalogSaysAdmin != isAdminOnly)
                    failures.Add($"YONETICI KATALOGU UYUSMUYOR: {method} {rawText} (katalog={catalogSaysAdmin}, kod={isAdminOnly})");

                if (catalogSaysAdmin)
                {
                    var staffResp = await SendAsync(staffClient, method, path, isMultipart: isMultipart);
                    if (staffResp.StatusCode != HttpStatusCode.Forbidden)
                        failures.Add($"403 BEKLENDI (Muhasebe, yonetici-ozel uc): {method} {path} -> {(int)staffResp.StatusCode}");
                }
            }
        }

        Assert.True(failures.Count == 0, "Yetki denetimi hatalari:\n" + string.Join("\n", failures));
    }

    private async Task<HttpClient> SetupAdminAsync()
    {
        var client = _factory.CreateClient();
        var statusResp = await SendAsync(client, "GET", "/api/setup/status");
        statusResp.EnsureSuccessStatusCode();
        var token = await ReadSetupTokenFromDiskAsync();

        var setupBody = new
        {
            organizationName = "Test Kurum", userName = AdminUser, fullName = "Test Admin",
            password = Password, token,
        };
        var setupResp = await SendAsync(client, "POST", "/api/setup", setupBody);
        Assert.True(setupResp.IsSuccessStatusCode, $"kurulum basarisiz: {await setupResp.Content.ReadAsStringAsync()}");

        await LoginAndEnableMfaAsync(client, AdminUser);
        return client;
    }

    private async Task<HttpClient> CreateStaffSessionAsync(HttpClient adminClient)
    {
        var createResp = await SendAsync(adminClient, "POST", "/api/users", new
        {
            userName = StaffUser, fullName = "Test Muhasebe", password = Password, role = "Muhasebe", maxDiscountPercent = 10,
        });
        Assert.True(createResp.IsSuccessStatusCode, $"personel olusturma basarisiz: {await createResp.Content.ReadAsStringAsync()}");

        var client = _factory.CreateClient();
        await LoginAndEnableMfaAsync(client, StaffUser);
        return client;
    }

    private async Task LoginAndEnableMfaAsync(HttpClient client, string userName)
    {
        var loginResp = await SendAsync(client, "POST", "/api/auth/login", new { userName, password = Password });
        loginResp.EnsureSuccessStatusCode();

        var setupResp = await SendAsync(client, "GET", "/api/auth/mfa/setup");
        setupResp.EnsureSuccessStatusCode();
        var setup = await setupResp.Content.ReadFromJsonAsync<JsonElement>();
        var sharedKey = setup.GetProperty("sharedKey").GetString()!.Replace(" ", "");
        var code = ComputeTotp(sharedKey);

        var enableResp = await SendAsync(client, "POST", "/api/auth/mfa/enable", new { code });
        Assert.True(enableResp.IsSuccessStatusCode, $"MFA acilamadi ({userName}): {await enableResp.Content.ReadAsStringAsync()}");
    }

    private async Task<string> ReadSetupTokenFromDiskAsync()
    {
        var path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "UUSport", "secrets", "setup-token.txt");
        for (var i = 0; i < 20 && !File.Exists(path); i++) await Task.Delay(50);
        return (await File.ReadAllTextAsync(path)).Trim();
    }

    private static async Task<HttpResponseMessage> SendAsync(HttpClient client, string method, string path,
        object? body = null, bool isMultipart = false)
    {
        var req = new HttpRequestMessage(new HttpMethod(method), path);
        if (method is not ("GET" or "HEAD" or "OPTIONS"))
        {
            req.Headers.Add(SecurityMiddleware.RequestHeader, "1");
            req.Content = isMultipart ? new MultipartFormDataContent() : JsonContent.Create(body ?? new { });
        }
        return await client.SendAsync(req);
    }

    private static string ResolvePath(RoutePattern pattern)
    {
        var text = pattern.RawText ?? "/";
        return Regex.Replace(text, @"\{([^:}]+)(:[^}]+)?\}", m =>
        {
            var constraint = m.Groups[2].Value;
            if (constraint.Contains("guid", StringComparison.OrdinalIgnoreCase)) return Guid.NewGuid().ToString();
            if (constraint.Contains("int", StringComparison.OrdinalIgnoreCase)) return "1";
            return "x";
        });
    }

    /// <summary>Identity'nin varsayilan RFC 6238 TOTP algoritmasiyla ayni (SHA1, 30 sn, 6 hane).</summary>
    private static string ComputeTotp(string base32Secret)
    {
        var key = Base32Decode(base32Secret);
        var counter = DateTimeOffset.UtcNow.ToUnixTimeSeconds() / 30;
        var counterBytes = BitConverter.GetBytes(counter);
        if (BitConverter.IsLittleEndian) Array.Reverse(counterBytes);
        using var hmac = new HMACSHA1(key);
        var hash = hmac.ComputeHash(counterBytes);
        var offset = hash[^1] & 0xf;
        var binary = ((hash[offset] & 0x7f) << 24) | ((hash[offset + 1] & 0xff) << 16)
            | ((hash[offset + 2] & 0xff) << 8) | (hash[offset + 3] & 0xff);
        return (binary % 1_000_000).ToString("D6");
    }

    private static byte[] Base32Decode(string input)
    {
        const string alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";
        var output = new List<byte>();
        int buffer = 0, bitsLeft = 0;
        foreach (var c in input.Trim().TrimEnd('=').ToUpperInvariant())
        {
            var val = alphabet.IndexOf(c);
            if (val < 0) continue;
            buffer = (buffer << 5) | val;
            bitsLeft += 5;
            if (bitsLeft >= 8)
            {
                output.Add((byte)((buffer >> (bitsLeft - 8)) & 0xff));
                bitsLeft -= 8;
            }
        }
        return output.ToArray();
    }

    private static async Task ExecAsync(string connectionString, string sql)
    {
        await using var conn = new NpgsqlConnection(connectionString);
        await conn.OpenAsync();
        await using var cmd = new NpgsqlCommand(sql, conn);
        await cmd.ExecuteNonQueryAsync();
    }
}
