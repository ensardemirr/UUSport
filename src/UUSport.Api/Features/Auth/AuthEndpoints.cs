using System.Text.Encodings.Web;
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
    /// <summary>Personel: rol + iki adimli dogrulama acik (O74).</summary>
    public const string Staff = "staff";
    public const string Admin = "admin";
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

public sealed record SetupInput(string OrganizationName, string UserName, string FullName, string Password);
public sealed record LoginInput(string UserName, string Password);
public sealed record CodeInput(string Code);

public static class AuthEndpoints
{
    public static void MapAuth(this IEndpointRouteBuilder app)
    {
        var setup = app.MapGroup("/api/setup");

        setup.MapGet("/status", async (AppDbContext db, CancellationToken ct) =>
            Results.Ok(new { needsSetup = !await db.Users.AnyAsync(ct) }));

        // Ilk yonetici kurulum aninda olusturulur; varsayilan parola yoktur (standart 2.3).
        setup.MapPost("", async (SetupInput input, AppDbContext db, UserManager<AppUser> users,
            RoleManager<AppRole> roles, TenantContext tenant, TenantResolver resolver, AuditWriter audit, CancellationToken ct) =>
        {
            if (await db.Users.AnyAsync(ct))
                throw new DomainException("setup_done", "Kurulum daha önce tamamlanmış.");
            var orgName = TextRules.CleanName(input.OrganizationName);
            if (orgName.Length == 0) throw new DomainException("org", "Kurum adı zorunlu.");

            var tenantEntity = await db.Tenants.FirstOrDefaultAsync(ct);
            if (tenantEntity is null)
            {
                tenantEntity = new Tenant { Name = orgName };
                db.Tenants.Add(tenantEntity);
                await db.SaveChangesAsync(ct);
            }
            tenant.TenantId = tenantEntity.Id;
            resolver.Set(tenantEntity.Id);

            foreach (var r in Roles.All)
                if (!await roles.RoleExistsAsync(r)) await roles.CreateAsync(new AppRole(r));

            var user = new AppUser
            {
                UserName = input.UserName.Trim().ToLowerInvariant(),
                FullName = TextRules.CleanName(input.FullName),
                TenantId = tenantEntity.Id,
                MaxDiscountPercent = 100,
            };
            var created = await users.CreateAsync(user, input.Password);
            if (!created.Succeeded) throw new DomainException("password", Describe(created));
            await users.AddToRoleAsync(user, Roles.Yonetici);

            // Varsayilan kasa hesaplari: kullanici sonra degistirebilir.
            db.CashAccounts.AddRange(
                new CashAccount { TenantId = tenantEntity.Id, Name = "Merkez Kasa", Kind = CashAccountKind.Cash },
                new CashAccount { TenantId = tenantEntity.Id, Name = "Banka", Kind = CashAccountKind.Bank },
                new CashAccount { TenantId = tenantEntity.Id, Name = "POS", Kind = CashAccountKind.Pos });
            audit.Add("setup.complete", "tenant", tenantEntity.Id, new { user = user.UserName });
            await db.SaveChangesAsync(ct);
            return Results.Ok(new { ok = true });
        }).RequireRateLimiting("auth");

        var auth = app.MapGroup("/api/auth");

        auth.MapPost("/login", async (LoginInput input, SignInManager<AppUser> signIn, UserManager<AppUser> users) =>
        {
            var user = await users.FindByNameAsync(input.UserName.Trim().ToLowerInvariant());
            if (user is null || !user.IsActive)
                return Results.Json(new { code = "invalid", message = "Kullanıcı adı veya parola hatalı." }, statusCode: 401);
            var result = await signIn.PasswordSignInAsync(user, input.Password, isPersistent: false, lockoutOnFailure: true);
            if (result.IsLockedOut)
                return Results.Json(new { code = "locked", message = "Çok sayıda hatalı deneme. Hesap geçici olarak kilitlendi." }, statusCode: 423);
            if (result.RequiresTwoFactor) return Results.Ok(new { status = "mfa_required" });
            if (!result.Succeeded)
                return Results.Json(new { code = "invalid", message = "Kullanıcı adı veya parola hatalı." }, statusCode: 401);
            return Results.Ok(new { status = user.TwoFactorEnabled ? "ok" : "mfa_setup_required" });
        }).RequireRateLimiting("auth");

        auth.MapPost("/login/mfa", async (CodeInput input, SignInManager<AppUser> signIn) =>
        {
            var code = new string(input.Code.Where(char.IsAsciiDigit).ToArray());
            var result = await signIn.TwoFactorAuthenticatorSignInAsync(code, isPersistent: false, rememberClient: false);
            if (result.IsLockedOut)
                return Results.Json(new { code = "locked", message = "Çok sayıda hatalı deneme. Hesap geçici olarak kilitlendi." }, statusCode: 423);
            if (!result.Succeeded)
                return Results.Json(new { code = "invalid_code", message = "Doğrulama kodu hatalı." }, statusCode: 401);
            return Results.Ok(new { status = "ok" });
        }).RequireRateLimiting("auth");

        auth.MapPost("/logout", async (SignInManager<AppUser> signIn) =>
        {
            await signIn.SignOutAsync();
            return Results.Ok(new { ok = true });
        });

        auth.MapGet("/me", async (HttpContext http, UserManager<AppUser> users) =>
        {
            var user = await users.GetUserAsync(http.User);
            if (user is null || !user.IsActive) return Results.Ok(new { authenticated = false });
            return Results.Ok(new
            {
                authenticated = true,
                userName = user.UserName,
                fullName = user.FullName,
                roles = await users.GetRolesAsync(user),
                maxDiscountPercent = user.MaxDiscountPercent,
                mfaEnabled = user.TwoFactorEnabled,
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
            await signIn.RefreshSignInAsync(user);
            audit.Add("user.mfa_enabled", "user", user.Id, tenantOverride: user.TenantId);
            await db.SaveChangesAsync();
            return Results.Ok(new { ok = true });
        }).RequireRateLimiting("auth");
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
