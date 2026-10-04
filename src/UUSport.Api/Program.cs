using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using UUSport.Api.Data;
using UUSport.Api.Domain;
using UUSport.Api.Features.Accounting;
using UUSport.Api.Features.Audit;
using UUSport.Api.Features.Auth;
using UUSport.Api.Features.Cash;
using UUSport.Api.Features.Catalog;
using UUSport.Api.Features.Imports;
using UUSport.Api.Features.Parties;
using UUSport.Api.Features.Reports;
using UUSport.Api.Features.Users;
using UUSport.Api.Infrastructure;

[assembly: System.Runtime.Versioning.SupportedOSPlatform("windows")]
[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("UUSport.Tests")]

var builder = WebApplication.CreateBuilder(args);
var dbOptions = builder.Configuration.GetSection("Database").Get<DatabaseOptions>() ?? new DatabaseOptions();

// ---------------------------------------------------------------------------
// migrate modu: yalniz migration rolu ile semayi gunceller ve cikar.
// Uygulama calisma zamani migration yapmaz (operasyon proseduru bolum 6-8).
// ---------------------------------------------------------------------------
if (args.Contains("migrate"))
{
    var database = args.FirstOrDefault(a => a.StartsWith("--database=", StringComparison.Ordinal))?["--database=".Length..];
    var options = new DbContextOptionsBuilder<AppDbContext>()
        .UseNpgsql(dbOptions.BuildConnectionString(asOwner: true, database))
        .UseSnakeCaseNamingConvention()
        .Options;
    await using var migrationDb = new AppDbContext(options);
    var pending = (await migrationDb.Database.GetPendingMigrationsAsync()).ToList();
    Console.WriteLine($"Veritabani: {database ?? dbOptions.Database} | bekleyen migration: {pending.Count}");
    foreach (var m in pending) Console.WriteLine($"  - {m}");
    await migrationDb.Database.MigrateAsync();
    var applied = (await migrationDb.Database.GetAppliedMigrationsAsync()).LastOrDefault();
    Console.WriteLine($"Migration seviyesi: {applied}");
    return;
}

// ---------------------------------------------------------------------------
// reset-mfa modu: acil durum (S-15) - hicbir yonetici giris yapamiyorsa (telefon/kurtarma
// kodu kaybi) sunucuya fiziksel/RDP erisimi olan biri bu komutla bir kullanicinin iki adimli
// dogrulamasini sifirlar. Web ucundan YAPILAMAZ; yalniz makine erisimi olan kisi calistirabilir.
// ---------------------------------------------------------------------------
if (args.Contains("reset-mfa"))
{
    var userName = args.FirstOrDefault(a => a.StartsWith("--user=", StringComparison.Ordinal))?["--user=".Length..];
    if (string.IsNullOrWhiteSpace(userName))
    {
        Console.WriteLine("Kullanim: UUSport.Api.exe reset-mfa --user=<kullanici_adi>");
        return;
    }
    var options = new DbContextOptionsBuilder<AppDbContext>()
        .UseNpgsql(dbOptions.BuildConnectionString(asOwner: true))
        .UseSnakeCaseNamingConvention()
        .Options;
    await using var resetDb = new AppDbContext(options);
    var normalized = userName.Trim().ToUpperInvariant();
    var target = await resetDb.Users.SingleOrDefaultAsync(u => u.NormalizedUserName == normalized);
    if (target is null)
    {
        Console.WriteLine($"Kullanici bulunamadi: {userName}");
        return;
    }
    target.TwoFactorEnabled = false;
    target.LockoutEnd = null;
    target.AccessFailedCount = 0;
    target.SecurityStamp = Guid.NewGuid().ToString("N");
    resetDb.UserTokens.RemoveRange(resetDb.UserTokens.Where(t => t.UserId == target.Id));
    await resetDb.SaveChangesAsync();
    Console.WriteLine($"'{userName}' icin iki adimli dogrulama sifirlandi ve tum oturumlar kapatildi. Sonraki giriste yeniden kurulum istenecek.");
    return;
}

// ---------------------------------------------------------------------------
// Servisler
// ---------------------------------------------------------------------------
var appConnection = dbOptions.BuildConnectionString(asOwner: false);
builder.Services.AddSingleton(dbOptions);
builder.Services.AddSingleton<AppBuildInfo>();
builder.Services.AddSingleton<SetupTokenService>();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<TenantContext>();
builder.Services.AddScoped<TenantConnectionInterceptor>();
builder.Services.AddSingleton<TenantResolver>();
builder.Services.AddDbContext<AppDbContext>((sp, o) => o
    .UseNpgsql(appConnection)
    .UseSnakeCaseNamingConvention()
    .AddInterceptors(sp.GetRequiredService<TenantConnectionInterceptor>()));

builder.Services.AddIdentity<AppUser, AppRole>(o =>
    {
        o.Password.RequiredLength = 12;
        o.Password.RequireDigit = true;
        o.Password.RequireNonAlphanumeric = false;
        o.Password.RequireUppercase = false;
        o.Password.RequireLowercase = false;
        o.Lockout.MaxFailedAccessAttempts = 5;
        o.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
        o.Lockout.AllowedForNewUsers = true;
        o.User.AllowedUserNameCharacters = "abcdefghijklmnopqrstuvwxyz0123456789._-";
        o.SignIn.RequireConfirmedAccount = false;
    })
    .AddEntityFrameworkStores<AppDbContext>()
    .AddDefaultTokenProviders();

builder.Services.Configure<SecurityStampValidatorOptions>(o => o.ValidationInterval = TimeSpan.FromMinutes(1));
builder.Services.ConfigureApplicationCookie(o =>
{
    o.Cookie.Name = "uusport.auth";
    o.Cookie.HttpOnly = true;
    o.Cookie.SameSite = SameSiteMode.Strict;
    o.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
    // Hareketsizlik zaman asimi (S-15): 30 dk islem yapilmazsa oturum kapanir. Kaydirmali oldugu
    // icin aktif kullanimda kesinti olmaz; muhasebe verisi acik ekranda uzun sure yalniz kalmaz.
    o.ExpireTimeSpan = TimeSpan.FromMinutes(30);
    o.SlidingExpiration = true;
    o.Events.OnRedirectToLogin = ctx => { ctx.Response.StatusCode = 401; return Task.CompletedTask; };
    o.Events.OnRedirectToAccessDenied = ctx => { ctx.Response.StatusCode = 403; return Task.CompletedTask; };
});

// "Bu tarayicidan hatirla" (AuthEndpoints /login/mfa, rememberClient:true): TOTP kurulumu bir
// kere yapilir, ayni tarayicida 30 gun boyunca kod tekrar sorulmaz. MFA'yi kapatmaz; yalniz
// gunluk giristeki tekrar kod yazma surtunmesini azaltir.
builder.Services.Configure<CookieAuthenticationOptions>(IdentityConstants.TwoFactorRememberMeScheme, o =>
{
    o.Cookie.Name = "uusport.mfa-remember";
    o.Cookie.HttpOnly = true;
    o.Cookie.SameSite = SameSiteMode.Strict;
    o.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
    o.ExpireTimeSpan = TimeSpan.FromDays(30);
});

builder.Services.AddScoped<IAuthorizationHandler, MfaEnabledHandler>();
builder.Services.AddAuthorizationBuilder()
    .AddPolicy(Policies.SignedIn, p => p.RequireAuthenticatedUser())
    .AddPolicy(Policies.Staff, p => p.RequireAuthenticatedUser()
        .RequireRole(Roles.Yonetici, Roles.Muhasebe).AddRequirements(new MfaEnabledRequirement()))
    .AddPolicy(Policies.Admin, p => p.RequireAuthenticatedUser()
        .RequireRole(Roles.Yonetici).AddRequirements(new MfaEnabledRequirement()));

builder.Services.AddRateLimiter(o =>
{
    o.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    o.AddPolicy("auth", ctx => RateLimitPartition.GetFixedWindowLimiter(
        ctx.Connection.RemoteIpAddress?.ToString() ?? "local",
        _ => new FixedWindowRateLimiterOptions { PermitLimit = 20, Window = TimeSpan.FromMinutes(1) }));
});

builder.Services.ConfigureHttpJsonOptions(o =>
{
    o.SerializerOptions.Converters.Add(new DecimalAsStringConverter());
    o.SerializerOptions.Converters.Add(new NullableDecimalAsStringConverter());
    o.SerializerOptions.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.SnakeCaseLower, allowIntegerValues: false));
});
builder.Services.Configure<FormOptions>(o => o.MultipartBodyLengthLimit = 16 * 1024 * 1024);

var keyDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "UUSport", "keys");
builder.Services.AddDataProtection()
    .SetApplicationName("UUSport")
    .PersistKeysToFileSystem(new DirectoryInfo(keyDirectory))
    .ProtectKeysWithDpapi();

builder.Services.AddScoped<ICurrentUser, HttpCurrentUser>();
builder.Services.AddScoped<AuditWriter>();
builder.Services.AddScoped<AccountingService>();
builder.Services.AddScoped<LoanService>();
builder.Services.AddScoped<CheckService>();
builder.Services.AddScoped<AccountingQueries>();
builder.Services.AddScoped<ImportService>();

// ---------------------------------------------------------------------------
// Istek hatti
// ---------------------------------------------------------------------------
var app = builder.Build();

// HTTPS/LAN (S-11): sunucu bir ic CA sertifikasiyla HTTPS'e gectiginde tarayicilar bu basligi
// hatirlar ve bir daha HTTP'ye dusmez. Duz HTTP uzerinden gonderilen bu baslik tarayicilarca
// yoksayilir (RFC 6797); yani gelistirme ortaminda (henuz HTTP) zararsizdir, HTTPS acilinca aktif olur.
if (!app.Environment.IsDevelopment()) app.UseHsts();

app.UseMiddleware<SecurityMiddleware>();
app.UseMiddleware<ErrorHandlingMiddleware>();
app.UseDefaultFiles();
app.UseStaticFiles();
app.UseRateLimiter();
app.Use(async (ctx, next) =>
{
    if (ctx.Request.Path.StartsWithSegments("/api"))
    {
        var tenant = ctx.RequestServices.GetRequiredService<TenantContext>();
        tenant.TenantId = await ctx.RequestServices.GetRequiredService<TenantResolver>().GetAsync(ctx.RequestAborted);
    }
    await next(ctx);
});
app.UseAuthentication();
app.UseAuthorization();

// Anonim erisimde yalniz "ok" doner (izleme/LAN kontrolu icin); surum ve migration bilgisi
// oturum acmis personele ayrilir (S-15, X:SEC-10 - surum ifsasi taramayi kolaylastirir).
app.MapGet("/api/health", async (HttpContext http, AppDbContext db, CancellationToken ct) =>
{
    if (http.User.Identity?.IsAuthenticated != true) return Results.Ok(new { status = "ok" });
    var level = (await db.Database.GetAppliedMigrationsAsync(ct)).LastOrDefault();
    return Results.Ok(new { status = "ok", version = typeof(Program).Assembly.GetName().Version?.ToString(), migration = level });
});
app.MapAuth();
app.MapUsers();
app.MapParties();
app.MapCatalog();
app.MapAccounting();
app.MapLoans();
app.MapChecks();
app.MapReports();
app.MapImports();
app.MapFallbackToFile("index.html");

app.Run();

public partial class Program;
