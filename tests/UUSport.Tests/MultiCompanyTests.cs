using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using UUSport.Api.Data;
using UUSport.Api.Features.Auth;
using UUSport.Api.Infrastructure;

namespace UUSport.Tests;

/// <summary>Ayni ana makinede ortam degiskeniyle veritabani secen WebApplicationFactory siniflari birlikte kosmamali.</summary>
[CollectionDefinition("WebHost")]
public sealed class WebHostCollection;

/// <summary>
/// Uc sirket (UUS, FIT, TIY) ve bes kullanici (ADR-012): patron, muhasebe A+B, muhasebe C, ortak B,
/// yonetici B. Tum denetimler gercek HTTP hatti ve uygulama rolu (RLS'e tabi) uzerinden yapilir.
/// </summary>
public sealed class MultiCompanyFixture : IAsyncLifetime
{
    private static readonly DatabaseOptions Options = new();
    public const string Password = "SifreTest12345678";

    private string _dbName = null!;
    private WebApplicationFactory<Program> _factory = null!;

    public Guid A { get; private set; }   // UUS
    public Guid B { get; private set; }   // FIT
    public Guid C { get; private set; }   // TIY
    public Guid UserOrtakB { get; private set; }
    public Guid UserAdminB { get; private set; }
    public Guid UserMuhasebeC { get; private set; }
    public Guid PartyA { get; private set; }
    public Guid PartyB { get; private set; }
    public Guid PartyC { get; private set; }
    public Dictionary<Guid, decimal> Receipts { get; } = [];

    public HttpClient Patron { get; private set; } = null!;
    public HttpClient MuhasebeAB { get; private set; } = null!;
    public HttpClient MuhasebeC { get; private set; } = null!;
    public HttpClient OrtakB { get; private set; } = null!;
    public HttpClient AdminB { get; private set; } = null!;

    public string AppConnection => Options.BuildConnectionString(asOwner: false, _dbName);

    public async Task InitializeAsync()
    {
        _dbName = "uusport_multitest_" + Guid.NewGuid().ToString("N")[..12];
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
            _ = _factory.Server;
        }
        finally
        {
            Environment.SetEnvironmentVariable("Database__Database", null);
        }

        // Patron: kurulum ilk sirketi (UUS) acar ve kullaniciyi grup yoneticisi yapar.
        Patron = _factory.CreateClient();
        Ok(await Send(Patron, "GET", "/api/setup/status")); // kurulum belirtecini uretir
        var token = await ReadSetupTokenAsync();
        Ok(await Send(Patron, "POST", "/api/setup", new
        {
            organizationName = "Uusport Test", userName = "patron", fullName = "Test Patron", password = Password, token,
            companyCode = "UUS",
        }));
        await LoginAndEnableMfaAsync(Patron, "patron");

        Ok(await Send(Patron, "POST", "/api/companies", new { name = "Fitness Test", code = "FIT" }));
        Ok(await Send(Patron, "POST", "/api/companies", new { name = "Tiyatro Test", code = "TIY" }));
        var all = await Json(Ok(await Send(Patron, "GET", "/api/companies/all")));
        Guid IdOf(string code) => all.EnumerateArray().Single(c => c.GetProperty("code").GetString() == code).GetProperty("id").GetGuid();
        A = IdOf("UUS"); B = IdOf("FIT"); C = IdOf("TIY");

        var muhasebeAB = await CreateUserAsync("muhasebe-ab", [(A, "Muhasebe"), (B, "Muhasebe")]);
        UserMuhasebeC = await CreateUserAsync("muhasebe-c", [(C, "Muhasebe")]);
        UserOrtakB = await CreateUserAsync("ortak-b", [(B, "Ortak")]);
        UserAdminB = await CreateUserAsync("yonetici-b", [(B, "Yonetici")]);
        _ = muhasebeAB;

        MuhasebeAB = await SessionAsync("muhasebe-ab");
        MuhasebeC = await SessionAsync("muhasebe-c");
        OrtakB = await SessionAsync("ortak-b");
        AdminB = await SessionAsync("yonetici-b");

        // Her sirkete birer cari ve farkli tutarda tahsilat (patron, tek sirket secili).
        (PartyA, Receipts[A]) = await SeedAsync(A, "Cari UUS", 100m);
        (PartyB, Receipts[B]) = await SeedAsync(B, "Cari FIT", 250m);
        (PartyC, Receipts[C]) = await SeedAsync(C, "Cari TIY", 400m);
    }

    public async Task DisposeAsync()
    {
        foreach (var c in new[] { Patron, MuhasebeAB, MuhasebeC, OrtakB, AdminB }) c?.Dispose();
        await _factory.DisposeAsync();
        NpgsqlConnection.ClearAllPools();
        var server = Options.BuildConnectionString(asOwner: true, "postgres");
        await ExecAsync(server, $"DROP DATABASE IF EXISTS {_dbName} WITH (FORCE)");
    }

    // ----- Yardimcilar -----

    private async Task<(Guid PartyId, decimal Amount)> SeedAsync(Guid company, string name, decimal amount)
    {
        var party = await Json(Ok(await Send(Patron, "POST", "/api/parties",
            new { name, kind = "customer" }, company)));
        var partyId = party.GetProperty("id").GetGuid();
        var accounts = await Json(Ok(await Send(Patron, "GET", "/api/cash-accounts", null, company)));
        var cashId = accounts.EnumerateArray().First(a => a.GetProperty("name").GetString() == "Merkez Kasa").GetProperty("id").GetGuid();
        Ok(await Send(Patron, "POST", "/api/payments", new
        {
            partyId, direction = "receipt", method = "cash", cashAccountId = cashId, amount, description = "test tahsilat",
        }, company));
        return (partyId, amount);
    }

    private async Task<Guid> CreateUserAsync(string userName, (Guid Tenant, string Role)[] memberships)
    {
        var resp = await Json(Ok(await Send(Patron, "POST", "/api/users", new
        {
            userName, fullName = "Test " + userName, password = Password, maxDiscountPercent = 10,
            memberships = memberships.Select(m => new { tenantId = m.Tenant, role = m.Role }).ToArray(),
        })));
        return resp.GetProperty("id").GetGuid();
    }

    private async Task<HttpClient> SessionAsync(string userName)
    {
        var client = _factory.CreateClient();
        await LoginAndEnableMfaAsync(client, userName);
        return client;
    }

    private async Task LoginAndEnableMfaAsync(HttpClient client, string userName)
    {
        Ok(await Send(client, "POST", "/api/auth/login", new { userName, password = Password }));
        var setup = await Json(Ok(await Send(client, "GET", "/api/auth/mfa/setup")));
        var sharedKey = setup.GetProperty("sharedKey").GetString()!.Replace(" ", "");
        Ok(await Send(client, "POST", "/api/auth/mfa/enable", new { code = ComputeTotp(sharedKey) }));
    }

    public static async Task<HttpResponseMessage> Send(HttpClient client, string method, string path, object? body = null,
        params Guid[] companies)
    {
        var req = new HttpRequestMessage(new HttpMethod(method), path);
        if (companies.Length > 0) req.Headers.Add(CompanyAccess.Header, string.Join(',', companies));
        if (method is not ("GET" or "HEAD" or "OPTIONS"))
        {
            req.Headers.Add(SecurityMiddleware.RequestHeader, "1");
            req.Content = JsonContent.Create(body ?? new { });
        }
        return await client.SendAsync(req);
    }

    /// <summary>"all" basligi ile (izinli her sirket) istek.</summary>
    public static async Task<HttpResponseMessage> SendAll(HttpClient client, string method, string path, object? body = null)
    {
        var req = new HttpRequestMessage(new HttpMethod(method), path);
        req.Headers.Add(CompanyAccess.Header, "all");
        if (method is not ("GET" or "HEAD" or "OPTIONS"))
        {
            req.Headers.Add(SecurityMiddleware.RequestHeader, "1");
            req.Content = JsonContent.Create(body ?? new { });
        }
        return await client.SendAsync(req);
    }

    public static HttpResponseMessage Ok(HttpResponseMessage resp)
    {
        if (!resp.IsSuccessStatusCode)
            throw new Xunit.Sdk.XunitException($"Beklenmeyen {(int)resp.StatusCode}: {resp.Content.ReadAsStringAsync().GetAwaiter().GetResult()}");
        return resp;
    }

    public static async Task<JsonElement> Json(HttpResponseMessage resp) => await resp.Content.ReadFromJsonAsync<JsonElement>();

    public static async Task<string?> CodeOf(HttpResponseMessage resp) =>
        (await resp.Content.ReadFromJsonAsync<JsonElement>()).TryGetProperty("code", out var c) ? c.GetString() : null;

    /// <summary>Para alanlari metin veya sayi olarak gelebilir (ADR-002); ikisi de ondaliga cevrilir.</summary>
    public static decimal Money(JsonElement e) =>
        decimal.Parse(e.ValueKind == JsonValueKind.String ? e.GetString()! : e.GetRawText(), CultureInfo.InvariantCulture);

    private static async Task<string> ReadSetupTokenAsync()
    {
        var path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "UUSport", "secrets", "setup-token.txt");
        for (var i = 0; i < 40 && !File.Exists(path); i++) await Task.Delay(50);
        return (await File.ReadAllTextAsync(path)).Trim();
    }

    private static string ComputeTotp(string base32Secret)
    {
        const string alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";
        var key = new List<byte>();
        int buffer = 0, bits = 0;
        foreach (var c in base32Secret.Trim().TrimEnd('=').ToUpperInvariant())
        {
            var v = alphabet.IndexOf(c);
            if (v < 0) continue;
            buffer = (buffer << 5) | v;
            bits += 5;
            if (bits >= 8) { key.Add((byte)((buffer >> (bits - 8)) & 0xff)); bits -= 8; }
        }
        var counter = BitConverter.GetBytes(DateTimeOffset.UtcNow.ToUnixTimeSeconds() / 30);
        if (BitConverter.IsLittleEndian) Array.Reverse(counter);
        using var hmac = new HMACSHA1(key.ToArray());
        var hash = hmac.ComputeHash(counter);
        var offset = hash[^1] & 0xf;
        var binary = ((hash[offset] & 0x7f) << 24) | ((hash[offset + 1] & 0xff) << 16)
            | ((hash[offset + 2] & 0xff) << 8) | (hash[offset + 3] & 0xff);
        return (binary % 1_000_000).ToString("D6");
    }

    private static async Task ExecAsync(string connectionString, string sql)
    {
        await using var conn = new NpgsqlConnection(connectionString);
        await conn.OpenAsync();
        await using var cmd = new NpgsqlCommand(sql, conn);
        await cmd.ExecuteNonQueryAsync();
    }
}

[Collection("WebHost")]
public sealed class MultiCompanyTests(MultiCompanyFixture w) : IClassFixture<MultiCompanyFixture>
{
    private static readonly object PartyBody = new { name = "Degisti", kind = "customer" };

    [Fact]
    public async Task MuhasebeC_BaskaSirketCarisini_Goremez()
    {
        // Kendi sirketinde cariyi gorur (pozitif kontrol), baska sirketinki "yok" gibi davranir.
        Assert.Equal(HttpStatusCode.OK, (await MultiCompanyFixture.Send(w.MuhasebeC, "GET", $"/api/parties/{w.PartyC}")).StatusCode);

        // Baslik vermese de ev sirketi kendisi; A'nin carisi RLS ile gorunmez.
        Assert.Equal(HttpStatusCode.NotFound, (await MultiCompanyFixture.Send(w.MuhasebeC, "GET", $"/api/parties/{w.PartyA}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await MultiCompanyFixture.Send(w.MuhasebeC, "PUT", $"/api/parties/{w.PartyA}", PartyBody)).StatusCode);

        // Baslikla A'yi secmeye calismak yetkisiz sirket oldugu icin 403 doner.
        var forced = await MultiCompanyFixture.Send(w.MuhasebeC, "GET", $"/api/parties/{w.PartyA}", null, w.A);
        Assert.Equal(HttpStatusCode.Forbidden, forced.StatusCode);
        Assert.Equal("company", await MultiCompanyFixture.CodeOf(forced));
    }

    [Fact]
    public async Task MuhasebeAB_YetkisizSirketBasligi_403()
    {
        var resp = await MultiCompanyFixture.Send(w.MuhasebeAB, "GET", "/api/parties", null, w.C);
        Assert.Equal(HttpStatusCode.Forbidden, resp.StatusCode);
        // A,B,C birlikte istenirse yalniz izinli kesisim (A,B) kalir; C verisi gelmez.
        var mixed = await MultiCompanyFixture.Json(MultiCompanyFixture.Ok(
            await MultiCompanyFixture.Send(w.MuhasebeAB, "GET", "/api/parties", null, w.A, w.B, w.C)));
        var names = mixed.EnumerateArray().Select(p => p.GetProperty("name").GetString()).ToList();
        Assert.Contains("Cari UUS", names);
        Assert.Contains("Cari FIT", names);
        Assert.DoesNotContain("Cari TIY", names);
    }

    [Fact]
    public async Task OrtakB_HerYazmaUcunda_ReadOnlyRole403()
    {
        var calls = new (string Method, string Path, object Body)[]
        {
            ("POST", "/api/parties", PartyBody),
            ("PUT", $"/api/parties/{w.PartyB}", PartyBody),
            ("POST", "/api/payments", new { partyId = w.PartyB, direction = "receipt", method = "cash", amount = 1m }),
            ("POST", "/api/documents", new { }),
            ("POST", "/api/opening-balances", new { partyId = w.PartyB, amount = 1m }),
        };
        foreach (var (method, path, body) in calls)
        {
            var resp = await MultiCompanyFixture.Send(w.OrtakB, method, path, body, w.B);
            Assert.True(resp.StatusCode == HttpStatusCode.Forbidden, $"{method} {path} -> {(int)resp.StatusCode}");
            Assert.Equal("read_only_role", await MultiCompanyFixture.CodeOf(resp));
        }

        // Ortak varsayilan olarak yalniz ozet raporu gorur.
        Assert.Equal(HttpStatusCode.OK, (await MultiCompanyFixture.Send(w.OrtakB, "GET", "/api/reports/daily-collections", null, w.B)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await MultiCompanyFixture.Send(w.OrtakB, "GET", "/api/parties", null, w.B)).StatusCode);
    }

    [Fact]
    public async Task BirlesikModdaYazma_Reddedilir_400Company()
    {
        var resp = await MultiCompanyFixture.Send(w.MuhasebeAB, "POST", "/api/parties", PartyBody, w.A, w.B);
        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
        Assert.Equal("company", await MultiCompanyFixture.CodeOf(resp));

        var all = await MultiCompanyFixture.SendAll(w.Patron, "POST", "/api/parties", PartyBody);
        Assert.Equal(HttpStatusCode.BadRequest, all.StatusCode);
        Assert.Equal("company", await MultiCompanyFixture.CodeOf(all));
    }

    [Fact]
    public async Task Rls_WithCheck_BaskaSirketeYazmayiVeritabaninda_Engeller()
    {
        await using var conn = new NpgsqlConnection(w.AppConnection);
        await conn.OpenAsync();

        // Okuma kumesi A,B; yazma sirketi A. B'nin satiri okunabilir ama degistirilemez.
        await using (var tx = await conn.BeginTransactionAsync())
        {
            await SetContextAsync(conn, tx, writeTenant: w.A, readTenants: [w.A, w.B]);
            await using var read = new NpgsqlCommand("SELECT count(*) FROM crm.party WHERE id = @b", conn, tx);
            read.Parameters.AddWithValue("b", w.PartyB);
            Assert.Equal(1L, await read.ExecuteScalarAsync());

            await using var update = new NpgsqlCommand("UPDATE crm.party SET name = name WHERE id = @b", conn, tx);
            update.Parameters.AddWithValue("b", w.PartyB);
            var ex = await Assert.ThrowsAsync<PostgresException>(() => update.ExecuteNonQueryAsync());
            Assert.Equal(PostgresErrorCodes.InsufficientPrivilege, ex.SqlState);
        }

        // Pozitif kontrol: yazma sirketi B olunca ayni guncelleme calisir.
        await using (var tx = await conn.BeginTransactionAsync())
        {
            await SetContextAsync(conn, tx, writeTenant: w.B, readTenants: [w.A, w.B]);
            await using var update = new NpgsqlCommand("UPDATE crm.party SET name = name WHERE id = @b", conn, tx);
            update.Parameters.AddWithValue("b", w.PartyB);
            Assert.Equal(1, await update.ExecuteNonQueryAsync());
        }

        // Okuma kumesinde olmayan sirketin satiri hic gorunmez.
        await using (var tx = await conn.BeginTransactionAsync())
        {
            await SetContextAsync(conn, tx, writeTenant: w.A, readTenants: [w.A]);
            await using var read = new NpgsqlCommand("SELECT count(*) FROM crm.party WHERE id = @b", conn, tx);
            read.Parameters.AddWithValue("b", w.PartyB);
            Assert.Equal(0L, await read.ExecuteScalarAsync());
        }
    }

    [Fact]
    public async Task SirketYoneticisi_BaskaSirketinKullanicisiniGormez()
    {
        // Yonetici B yalniz FIT'teki kullanicilari listeler.
        var list = await MultiCompanyFixture.Json(MultiCompanyFixture.Ok(await MultiCompanyFixture.Send(w.AdminB, "GET", "/api/users")));
        var names = list.EnumerateArray().Select(u => u.GetProperty("userName").GetString()).ToList();
        Assert.Contains("ortak-b", names);
        Assert.Contains("muhasebe-ab", names);
        Assert.Contains("yonetici-b", names);
        Assert.DoesNotContain("muhasebe-c", names);
        Assert.DoesNotContain("patron", names);

        // Baska sirketin kullanicisini degistirmeye calisinca 404 (varligi sizmaz).
        var put = await MultiCompanyFixture.Send(w.AdminB, "PUT", $"/api/users/{w.UserMuhasebeC}",
            new { fullName = "Hack", maxDiscountPercent = 10, isActive = true });
        Assert.Equal(HttpStatusCode.NotFound, put.StatusCode);

        // Grup yoneticisi hepsini gorur.
        var patronList = await MultiCompanyFixture.Json(MultiCompanyFixture.Ok(await MultiCompanyFixture.Send(w.Patron, "GET", "/api/users")));
        Assert.Contains("muhasebe-c", patronList.EnumerateArray().Select(u => u.GetProperty("userName").GetString()));
    }

    [Fact]
    public async Task Ortak_LedgerRead_VerilinceAcilir_AlinincaKapanir()
    {
        Assert.Equal(HttpStatusCode.Forbidden, (await MultiCompanyFixture.Send(w.OrtakB, "GET", "/api/ledger", null, w.B)).StatusCode);

        await SetOrtakKeysAsync(["ledger.read"]);
        Assert.Equal(HttpStatusCode.OK, (await MultiCompanyFixture.Send(w.OrtakB, "GET", "/api/ledger", null, w.B)).StatusCode);
        // Yetki ledger.read ile sinirli: cari listesi hala kapali, yazma yine yasak.
        Assert.Equal(HttpStatusCode.Forbidden, (await MultiCompanyFixture.Send(w.OrtakB, "GET", "/api/parties", null, w.B)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await MultiCompanyFixture.Send(w.OrtakB, "POST", "/api/parties", PartyBody, w.B)).StatusCode);

        await SetOrtakKeysAsync([]);
        Assert.Equal(HttpStatusCode.Forbidden, (await MultiCompanyFixture.Send(w.OrtakB, "GET", "/api/ledger", null, w.B)).StatusCode);
    }

    [Fact]
    public async Task Ortak_ICin_WriteAnahtari_Verilemez()
    {
        var resp = await MultiCompanyFixture.Send(w.Patron, "PUT", $"/api/users/{w.UserOrtakB}", new
        {
            fullName = "Test ortak-b", maxDiscountPercent = 10, isActive = true,
            memberships = new[] { new { tenantId = w.B, role = "Ortak", grantedKeys = new[] { "write" }, deniedKeys = Array.Empty<string>() } },
        });
        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
    }

    [Fact]
    public async Task BirlesikRapor_Toplami_SirketToplamlarininToplamidir()
    {
        decimal Payable(JsonElement e) => MultiCompanyFixture.Money(e.GetProperty("totalPayable"));

        var perCompany = new Dictionary<Guid, decimal>();
        foreach (var id in new[] { w.A, w.B, w.C })
            perCompany[id] = Payable(await MultiCompanyFixture.Json(MultiCompanyFixture.Ok(
                await MultiCompanyFixture.Send(w.Patron, "GET", "/api/reports/open-balances", null, id))));

        // Tahsilat onceden alinmis tutar (avans) olarak borc-alacak bakiyesinde gorunur.
        Assert.Equal(w.Receipts[w.A], perCompany[w.A]);
        Assert.Equal(w.Receipts[w.B], perCompany[w.B]);
        Assert.Equal(w.Receipts[w.C], perCompany[w.C]);

        var combined = await MultiCompanyFixture.Json(MultiCompanyFixture.Ok(
            await MultiCompanyFixture.SendAll(w.Patron, "GET", "/api/reports/open-balances")));
        Assert.Equal(perCompany.Values.Sum(), Payable(combined));
        Assert.Equal(3, combined.GetProperty("rows").GetArrayLength());

        // Gunluk tahsilat ozeti de ayni kuralla toplanir.
        decimal Total(JsonElement e) => MultiCompanyFixture.Money(e.GetProperty("total"));
        var dailyEach = 0m;
        foreach (var id in new[] { w.A, w.B, w.C })
            dailyEach += Total(await MultiCompanyFixture.Json(MultiCompanyFixture.Ok(
                await MultiCompanyFixture.Send(w.Patron, "GET", "/api/reports/daily-collections", null, id))));
        var dailyAll = Total(await MultiCompanyFixture.Json(MultiCompanyFixture.Ok(
            await MultiCompanyFixture.SendAll(w.Patron, "GET", "/api/reports/daily-collections"))));
        Assert.Equal(dailyEach, dailyAll);
    }

    [Fact]
    public async Task BirlesikListeler_SatirlaraSirketKoduEkler_SessizBirlestirmeYok()
    {
        var expected = new Dictionary<string, string> { ["Cari UUS"] = "UUS", ["Cari FIT"] = "FIT", ["Cari TIY"] = "TIY" };

        // Cari listesi ve hesap hareketleri: her satir kendi sirketinin kodunu tasir.
        var parties = await MultiCompanyFixture.Json(MultiCompanyFixture.Ok(await MultiCompanyFixture.SendAll(w.Patron, "GET", "/api/parties")));
        foreach (var p in parties.EnumerateArray())
            Assert.Equal(expected[p.GetProperty("name").GetString()!], p.GetProperty("company").GetString());

        var ledger = await MultiCompanyFixture.Json(MultiCompanyFixture.Ok(await MultiCompanyFixture.SendAll(w.Patron, "GET", "/api/ledger")));
        var ledgerRows = ledger.EnumerateArray().ToList();
        Assert.Equal(3, ledgerRows.Count);
        foreach (var r in ledgerRows)
            Assert.Equal(expected[r.GetProperty("partyName").GetString()!], r.GetProperty("company").GetString());

        // Tahsilatlar ve kasa hesaplari da sirket kodu tasir; tek sirket secilince yalniz o sirketin kodu gelir.
        var accounts = await MultiCompanyFixture.Json(MultiCompanyFixture.Ok(await MultiCompanyFixture.SendAll(w.Patron, "GET", "/api/cash-accounts")));
        Assert.Equal(["FIT", "TIY", "UUS"], accounts.EnumerateArray().Select(a => a.GetProperty("company").GetString()!).Distinct().Order().ToArray());
        var onlyB = await MultiCompanyFixture.Json(MultiCompanyFixture.Ok(await MultiCompanyFixture.Send(w.Patron, "GET", "/api/ledger", null, w.B)));
        Assert.All(onlyB.EnumerateArray(), r => Assert.Equal("FIT", r.GetProperty("company").GetString()));

        // Gunluk tahsilat: ayni gun/yontem/hesap adi ("Merkez Kasa") uc sirkette ayri satir kalir.
        var daily = await MultiCompanyFixture.Json(MultiCompanyFixture.Ok(await MultiCompanyFixture.SendAll(w.Patron, "GET", "/api/reports/daily-collections")));
        Assert.Equal(["FIT", "TIY", "UUS"], daily.GetProperty("rows").EnumerateArray().Select(r => r.GetProperty("company").GetString()!).Order().ToArray());
        var dailyByCompany = daily.GetProperty("totalsByCompany").EnumerateArray()
            .ToDictionary(t => t.GetProperty("company").GetString()!, t => MultiCompanyFixture.Money(t.GetProperty("total")));
        Assert.Equal(w.Receipts[w.A], dailyByCompany["UUS"]);
        Assert.Equal(w.Receipts[w.B], dailyByCompany["FIT"]);
        Assert.Equal(w.Receipts[w.C], dailyByCompany["TIY"]);
        Assert.Equal(dailyByCompany.Values.Sum(), MultiCompanyFixture.Money(daily.GetProperty("total")));

        // Acik bakiye ve pano: sirket kirilimi var, genel toplam kirilimlarin toplamidir.
        var open = await MultiCompanyFixture.Json(MultiCompanyFixture.Ok(await MultiCompanyFixture.SendAll(w.Patron, "GET", "/api/reports/open-balances")));
        Assert.All(open.GetProperty("rows").EnumerateArray(), r => Assert.Equal(expected[r.GetProperty("name").GetString()!], r.GetProperty("company").GetString()));
        var payableByCompany = open.GetProperty("totalsByCompany").EnumerateArray()
            .ToDictionary(t => t.GetProperty("company").GetString()!, t => MultiCompanyFixture.Money(t.GetProperty("totalPayable")));
        Assert.Equal(w.Receipts[w.B], payableByCompany["FIT"]);
        Assert.Equal(MultiCompanyFixture.Money(open.GetProperty("totalPayable")), payableByCompany.Values.Sum());

        var dash = await MultiCompanyFixture.Json(MultiCompanyFixture.Ok(await MultiCompanyFixture.SendAll(w.Patron, "GET", "/api/dashboard")));
        var dashCompanies = dash.GetProperty("totalsByCompany").EnumerateArray().ToList();
        Assert.Equal(["FIT", "TIY", "UUS"], dashCompanies.Select(t => t.GetProperty("company").GetString()!).Order().ToArray());
        Assert.Equal(MultiCompanyFixture.Money(dash.GetProperty("receiptsThisMonth")),
            dashCompanies.Sum(t => MultiCompanyFixture.Money(t.GetProperty("receiptsThisMonth"))));

        // Yetkisiz sirket kodu sizmaz: muhasebe C panoda yalniz kendi sirketini gorur.
        var dashC = await MultiCompanyFixture.Json(MultiCompanyFixture.Ok(await MultiCompanyFixture.SendAll(w.MuhasebeC, "GET", "/api/dashboard")));
        Assert.Equal(["TIY"], dashC.GetProperty("totalsByCompany").EnumerateArray().Select(t => t.GetProperty("company").GetString()!).ToArray());
    }

    private async Task SetOrtakKeysAsync(string[] granted)
    {
        var resp = await MultiCompanyFixture.Send(w.Patron, "PUT", $"/api/users/{w.UserOrtakB}", new
        {
            fullName = "Test ortak-b", maxDiscountPercent = 10, isActive = true,
            memberships = new[] { new { tenantId = w.B, role = "Ortak", grantedKeys = granted, deniedKeys = Array.Empty<string>() } },
        });
        MultiCompanyFixture.Ok(resp);
    }

    private static async Task SetContextAsync(NpgsqlConnection conn, NpgsqlTransaction tx, Guid writeTenant, Guid[] readTenants)
    {
        await using var cmd = new NpgsqlCommand(
            "SELECT set_config('app.tenant_id', @w, true), set_config('app.tenant_ids', @r, true)", conn, tx);
        cmd.Parameters.AddWithValue("w", writeTenant.ToString());
        cmd.Parameters.AddWithValue("r", string.Join(',', readTenants));
        await cmd.ExecuteNonQueryAsync();
    }
}
