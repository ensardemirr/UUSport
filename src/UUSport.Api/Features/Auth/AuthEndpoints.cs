using System.Text.Encodings.Web;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using UUSport.Api.Data;
using UUSport.Api.Domain;
using UUSport.Api.Features.Audit;
using UUSport.Api.Infrastructure;

namespace UUSport.Api.Features.Auth;

public static class Policies
{
    /// <summary>Personel: secili her sirkette uyelik + iki adimli dogrulama acik (O74, ADR-012).</summary>
    public const string Staff = "staff";
    /// <summary>Secili her sirkette Yonetici veya grup yoneticisi.</summary>
    public const string Admin = "admin";
    /// <summary>Grup yoneticisi (patron): sirket ve grup duzeyinde kullanici yonetimi.</summary>
    public const string GroupAdmin = "group-admin";
    /// <summary>Yalniz oturum (iki adimli dogrulama kurulumu icin).</summary>
    public const string SignedIn = "signed-in";
}

public sealed class MfaEnabledRequirement : IAuthorizationRequirement;

public sealed class MfaEnabledHandler(UserManager<AppUser> users) : AuthorizationHandler<MfaEnabledRequirement>
{
    protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, MfaEnabledRequirement requirement)
    {
        if (context.User.Identity?.IsAuthenticated != true) return;
        var user = await users.GetUserAsync(context.User);
        if (user is { IsActive: true, TwoFactorEnabled: true }) context.Succeed(requirement);
    }
}

public sealed record SetupInput(string OrganizationName, string UserName, string FullName, string Password, string Token,
    string? CompanyCode = null);
public sealed record LoginInput(string UserName, string Password);
public sealed record CodeInput(string Code);

public static class AuthEndpoints
{
    public static void MapAuth(this IEndpointRouteBuilder app)
    {
        var setup = app.MapGroup("/api/setup");

        setup.MapGet("/status", async (AppDbContext db, SetupTokenService tokens, CancellationToken ct) =>
        {
            var needsSetup = !await db.Users.AnyAsync(ct);
            if (needsSetup) tokens.EnsureToken();
            return Results.Ok(new { needsSetup });
        });

        // Ilk yonetici kurulum aninda olusturulur; varsayilan parola yoktur (standart 2.3).
        // Kurulum jetonu (S-12): sunucu makinesinde uretilir, ag/internetteki baskasi ilk
        // yonetici hesabini calamaz.
        setup.MapPost("", async (SetupInput input, AppDbContext db, UserManager<AppUser> users,
            TenantContext tenant, TenantResolver resolver, AuditWriter audit,
            SetupTokenService tokens, CancellationToken ct) =>
        {
            if (await db.Users.AnyAsync(ct))
                throw new DomainException("setup_done", "Kurulum daha önce tamamlanmış.");
            if (!tokens.Validate(input.Token))
                throw new DomainException("token", "Kurulum jetonu hatalı veya süresi geçmiş. Sunucu logundaki jetonu girin.");
            var orgName = TextRules.CleanName(input.OrganizationName);
            if (orgName.Length == 0) throw new DomainException("org", "Kurum adı zorunlu.");

            var tenantEntity = await db.Tenants.OrderBy(t => t.CreatedAt).FirstOrDefaultAsync(ct);
            if (tenantEntity is null)
            {
                tenantEntity = new Tenant
                {
                    Name = orgName,
                    Code = string.IsNullOrWhiteSpace(input.CompanyCode)
                        ? Companies.CompanyCodes.Derive(orgName)
                        : Companies.CompanyCodes.Normalize(input.CompanyCode),
                };
                db.Tenants.Add(tenantEntity);
                await db.SaveChangesAsync(ct);
            }
            tenant.TenantId = tenantEntity.Id;
            tenant.ReadTenantIds = [tenantEntity.Id];
            resolver.Set(tenantEntity.Id);

            // Ilk kullanici grup yoneticisidir (patron): tum sirketleri gorur, sirket ve uyelik yonetir (ADR-012).
            var user = new AppUser
            {
                UserName = input.UserName.Trim().ToLowerInvariant(),
                FullName = TextRules.CleanName(input.FullName),
                TenantId = tenantEntity.Id,
                MaxDiscountPercent = 100,
                IsGroupAdmin = true,
            };
            var created = await users.CreateAsync(user, input.Password);
            if (!created.Succeeded) throw new DomainException("password", Describe(created));
            db.CompanyMemberships.Add(new CompanyMembership
            {
                UserId = user.Id, TenantId = tenantEntity.Id, Role = Roles.Yonetici, GrantedByUserId = user.Id,
            });

            // Varsayilan kasa hesaplari: kullanici sonra degistirebilir.
            db.CashAccounts.AddRange(
                new CashAccount { TenantId = tenantEntity.Id, Name = "Merkez Kasa", Kind = CashAccountKind.Cash },
                new CashAccount { TenantId = tenantEntity.Id, Name = "Banka", Kind = CashAccountKind.Bank },
                new CashAccount { TenantId = tenantEntity.Id, Name = "POS", Kind = CashAccountKind.Pos });
            audit.Add("setup.complete", "tenant", tenantEntity.Id, new { user = user.UserName });
            await db.SaveChangesAsync(ct);
            tokens.Consume();
            return Results.Ok(new { ok = true });
        }).RequireRateLimiting("auth");

        var auth = app.MapGroup("/api/auth");

        auth.MapPost("/login", async (LoginInput input, SignInManager<AppUser> signIn, UserManager<AppUser> users,
            AppDbContext db, TenantContext tenant, HttpContext http) =>
        {
            var name = input.UserName.Trim().ToLowerInvariant();
            var user = await users.FindByNameAsync(name);
            if (user is null || !user.IsActive)
            {
                await LogAuthAsync(db, tenant, http, null, "auth.login_failed", new { userName = name });
                return Results.Json(new { code = "invalid", message = "Kullanıcı adı veya parola hatalı." }, statusCode: 401);
            }
            // "Bu tarayicidan hatirla" (asagida /login/mfa, rememberClient:true) daha once
            // isaretlenmisse Identity RequiresTwoFactor'u burada otomatik olarak false doner.
            var result = await signIn.PasswordSignInAsync(user, input.Password, isPersistent: false, lockoutOnFailure: true);
            if (result.IsLockedOut)
            {
                await LogAuthAsync(db, tenant, http, user, "auth.locked", null);
                return Results.Json(new { code = "locked", message = "Çok sayıda hatalı deneme. Hesap geçici olarak kilitlendi." }, statusCode: 423);
            }
            if (result.RequiresTwoFactor) return Results.Ok(new { status = "mfa_required" });
            if (!result.Succeeded)
            {
                await LogAuthAsync(db, tenant, http, user, "auth.login_failed", null);
                return Results.Json(new { code = "invalid", message = "Kullanıcı adı veya parola hatalı." }, statusCode: 401);
            }
            await LogAuthAsync(db, tenant, http, user, "auth.login_success", null);
            return Results.Ok(new { status = user.TwoFactorEnabled ? "ok" : "mfa_setup_required" });
        }).RequireRateLimiting("auth");

        auth.MapPost("/login/mfa", async (CodeInput input, SignInManager<AppUser> signIn,
            AppDbContext db, TenantContext tenant, HttpContext http) =>
        {
            var pending = await signIn.GetTwoFactorAuthenticationUserAsync();
            var code = new string(input.Code.Where(char.IsAsciiDigit).ToArray());
            // rememberClient:true -> bu tarayicida 30 gun boyunca kod tekrar sorulmaz (asagida
            // TwoFactorRememberMeScheme cookie suresi). MFA kapatilmaz; yalniz ayni cihaz/tarayicida
            // gunluk kullanimdaki tekrar kod girme surtunmesi azalir.
            var result = await signIn.TwoFactorAuthenticatorSignInAsync(code, isPersistent: false, rememberClient: true);
            if (result.IsLockedOut)
            {
                await LogAuthAsync(db, tenant, http, pending, "auth.locked", null);
                return Results.Json(new { code = "locked", message = "Çok sayıda hatalı deneme. Hesap geçici olarak kilitlendi." }, statusCode: 423);
            }
            if (!result.Succeeded)
            {
                await LogAuthAsync(db, tenant, http, pending, "auth.mfa_failed", null);
                return Results.Json(new { code = "invalid_code", message = "Doğrulama kodu hatalı." }, statusCode: 401);
            }
            await LogAuthAsync(db, tenant, http, pending, "auth.mfa_success", null);
            return Results.Ok(new { status = "ok" });
        }).RequireRateLimiting("auth");

        // Telefon/uygulama kaybedildiginde kurtarma kodu ile giris (S-15).
        auth.MapPost("/login/mfa/recovery", async (CodeInput input, SignInManager<AppUser> signIn,
            AppDbContext db, TenantContext tenant, HttpContext http) =>
        {
            var pending = await signIn.GetTwoFactorAuthenticationUserAsync();
            var result = await signIn.TwoFactorRecoveryCodeSignInAsync(input.Code.Trim());
            if (result.IsLockedOut)
            {
                await LogAuthAsync(db, tenant, http, pending, "auth.locked", null);
                return Results.Json(new { code = "locked", message = "Çok sayıda hatalı deneme. Hesap geçici olarak kilitlendi." }, statusCode: 423);
            }
            if (!result.Succeeded)
            {
                await LogAuthAsync(db, tenant, http, pending, "auth.recovery_failed", null);
                return Results.Json(new { code = "invalid_code", message = "Kurtarma kodu hatalı veya daha önce kullanılmış." }, statusCode: 401);
            }
            await LogAuthAsync(db, tenant, http, pending, "auth.recovery_used", null);
            return Results.Ok(new { status = "ok" });
        }).RequireRateLimiting("auth");

        auth.MapPost("/logout", async (SignInManager<AppUser> signIn, UserManager<AppUser> users,
            AppDbContext db, TenantContext tenant, HttpContext http) =>
        {
            var user = await users.GetUserAsync(http.User);
            await signIn.SignOutAsync();
            await LogAuthAsync(db, tenant, http, user, "auth.logout", null);
            return Results.Ok(new { ok = true });
        });

        auth.MapGet("/me", async (HttpContext http, UserManager<AppUser> users, AppDbContext db, CompanyAccess access) =>
        {
            var user = await users.GetUserAsync(http.User);
            if (user is null || !user.IsActive) return Results.Ok(new { authenticated = false });
            // Secili sirketlerdeki etkin roller; arayuz menuyu buna gore kurar (yetki yine sunucudadir).
            var selectedGrants = access.Selected.Select(access.GrantFor).Where(g => g is not null).Select(g => g!).ToList();
            var roles = selectedGrants.Select(g => g.Role).Distinct().ToList();
            // Tek grup yoneticisi uyarisi (S-15): o kisi hesabindan cikamazsa/parolasini kaybederse
            // kimse grup islemi yapamaz. Sunucu tarafinda sayilir, sahte bir sayimla atlatilamaz.
            var onlyAdmin = user.IsGroupAdmin && await db.Users.CountAsync(u => u.IsActive && u.IsGroupAdmin) < 2;
            return Results.Ok(new
            {
                authenticated = true,
                userName = user.UserName,
                fullName = user.FullName,
                roles,
                isGroupAdmin = user.IsGroupAdmin,
                isAdmin = access.IsAdminInAllSelected(),
                canWrite = access.Selected.Count == 1 && access.HasInAllSelected(Permissions.Write),
                permissions = Permissions.All.Where(access.HasInAllSelected),
                companies = access.Allowed.Select(a => new { id = a.TenantId, a.Code, a.Name, a.Role, permissions = a.Permissions.OrderBy(p => p) }),
                selectedCompanyIds = access.Selected,
                maxDiscountPercent = user.MaxDiscountPercent,
                mfaEnabled = user.TwoFactorEnabled,
                onlyAdmin,
            });
        });

        var mfa = app.MapGroup("/api/auth/mfa").RequireAuthorization(Policies.SignedIn);

        mfa.MapGet("/setup", async (HttpContext http, UserManager<AppUser> users) =>
        {
            var user = await users.GetUserAsync(http.User) ?? throw new NotFoundException("Kullanıcı");
            if (user.TwoFactorEnabled) throw new DomainException("mfa_enabled", "İki adımlı doğrulama zaten açık.");
            var key = await users.GetAuthenticatorKeyAsync(user);
            if (string.IsNullOrEmpty(key))
            {
                await users.ResetAuthenticatorKeyAsync(user);
                key = await users.GetAuthenticatorKeyAsync(user);
            }
            var issuer = UrlEncoder.Default.Encode("UU Sport");
            var account = UrlEncoder.Default.Encode(user.UserName!);
            var uri = $"otpauth://totp/{issuer}:{account}?secret={key}&issuer={issuer}&digits=6";
            return Results.Ok(new { sharedKey = FormatKey(key!), authenticatorUri = uri });
        });

        mfa.MapPost("/enable", async (CodeInput input, HttpContext http, UserManager<AppUser> users,
            SignInManager<AppUser> signIn, AuditWriter audit, AppDbContext db, TenantContext tenant) =>
        {
            var user = await users.GetUserAsync(http.User) ?? throw new NotFoundException("Kullanıcı");
            var code = new string(input.Code.Where(char.IsAsciiDigit).ToArray());
            var valid = await users.VerifyTwoFactorTokenAsync(user, users.Options.Tokens.AuthenticatorTokenProvider, code);
            if (!valid) throw new DomainException("invalid_code", "Doğrulama kodu hatalı. Telefonunuzdaki saatin doğru olduğundan emin olun.");
            await users.SetTwoFactorEnabledAsync(user, true);
            var codes = await users.GenerateNewTwoFactorRecoveryCodesAsync(user, 10);
            await signIn.RefreshSignInAsync(user);
            audit.Add("user.mfa_enabled", "user", user.Id, tenantOverride: user.TenantId);
            await db.SaveChangesAsync();
            return Results.Ok(new { ok = true, recoveryCodes = codes });
        }).RequireRateLimiting("auth");

        // Kurtarma kodlari tukendiginde veya sifirlanmasi istendiginde yeniden uretilir; eskileri gecersiz olur.
        mfa.MapPost("/recovery/regenerate", async (HttpContext http, UserManager<AppUser> users,
            AuditWriter audit, AppDbContext db) =>
        {
            var user = await users.GetUserAsync(http.User) ?? throw new NotFoundException("Kullanıcı");
            if (!user.TwoFactorEnabled) throw new DomainException("mfa_disabled", "Önce iki adımlı doğrulamayı açmalısınız.");
            var codes = await users.GenerateNewTwoFactorRecoveryCodesAsync(user, 10);
            audit.Add("user.recovery_codes_regenerated", "user", user.Id, tenantOverride: user.TenantId);
            await db.SaveChangesAsync();
            return Results.Ok(new { recoveryCodes = codes });
        }).RequireRateLimiting("auth");
    }

    /// <summary>
    /// Giris/cikis/kilit/MFA denetimi (guvenlik standardi S-13): AuditWriter kullanmaz cunku
    /// bu olaylar cogu zaman henuz oturum acmamis (ICurrentUser bos) bir istek sirasinda olusur.
    /// IP adresi guvenlik sorusturmasi icin gerekli; kisisel veri (ad, telefon) tasimaz.
    /// </summary>
    private static async Task LogAuthAsync(AppDbContext db, TenantContext tenant, HttpContext http,
        AppUser? user, string action, object? extra)
    {
        var tenantId = user?.TenantId ?? tenant.TenantId;
        if (tenantId is null) return;
        db.AuditEvents.Add(new AuditEvent
        {
            TenantId = tenantId.Value,
            UserId = user?.Id,
            UserName = user?.UserName,
            Action = action,
            TargetType = "user",
            TargetId = user?.Id,
            CorrelationId = http.TraceIdentifier,
            IpAddress = http.Connection.RemoteIpAddress?.ToString(),
            Summary = extra is null ? null : JsonSerializer.Serialize(extra),
        });
        await db.SaveChangesAsync();
    }

    private static string FormatKey(string key) =>
        string.Join(' ', Enumerable.Range(0, (key.Length + 3) / 4).Select(i => key.Substring(i * 4, Math.Min(4, key.Length - i * 4))))
            .ToLowerInvariant();

    public static string Describe(IdentityResult result) =>
        string.Join(" ", result.Errors.Select(e => e.Code switch
        {
            "PasswordTooShort" => "Parola en az 12 karakter olmalı.",
            "PasswordRequiresDigit" => "Parola en az bir rakam içermeli.",
            "PasswordRequiresLower" => "Parola en az bir küçük harf içermeli.",
            "DuplicateUserName" => "Bu kullanıcı adı zaten kullanılıyor.",
            "InvalidUserName" => "Kullanıcı adı yalnız harf, rakam ve . _ - içerebilir.",
            _ => e.Description,
        }));
}
