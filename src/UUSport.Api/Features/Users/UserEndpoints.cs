using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using UUSport.Api.Data;
using UUSport.Api.Domain;
using UUSport.Api.Features.Audit;
using UUSport.Api.Features.Auth;
using UUSport.Api.Infrastructure;

namespace UUSport.Api.Features.Users;

public sealed record UserCreateInput(string UserName, string FullName, string Password, string Role, decimal MaxDiscountPercent);
public sealed record UserUpdateInput(string FullName, string Role, decimal MaxDiscountPercent, bool IsActive);
public sealed record PasswordInput(string Password);

public static class UserEndpoints
{
    public static void MapUsers(this IEndpointRouteBuilder app)
    {
        var g = app.MapGroup("/api/users").RequireAuthorization(Policies.Admin);

        g.MapGet("", async (AppDbContext db, UserManager<AppUser> users, CancellationToken ct) =>
        {
            var list = await db.Users.OrderBy(u => u.UserName).ToListAsync(ct);
            var result = new List<object>();
            foreach (var u in list)
            {
                result.Add(new
                {
                    u.Id, u.UserName, u.FullName, u.MaxDiscountPercent, u.IsActive,
                    mfaEnabled = u.TwoFactorEnabled, locked = u.LockoutEnd > DateTimeOffset.UtcNow,
                    roles = await users.GetRolesAsync(u),
                });
            }
            return Results.Ok(result);
        });

        g.MapPost("", async (UserCreateInput input, UserManager<AppUser> users, TenantContext tenant,
            AuditWriter audit, AppDbContext db) =>
        {
            ValidateRole(input.Role);
            ValidateDiscount(input.MaxDiscountPercent);
            var user = new AppUser
            {
                UserName = input.UserName.Trim().ToLowerInvariant(),
                FullName = TextRules.CleanName(input.FullName),
                TenantId = tenant.Require(),
                MaxDiscountPercent = input.MaxDiscountPercent,
            };
            if (user.FullName.Length == 0) throw new DomainException("full_name", "Ad soyad zorunlu.");
            var result = await users.CreateAsync(user, input.Password);
            if (!result.Succeeded) throw new DomainException("user", AuthEndpoints.Describe(result));
            await users.AddToRoleAsync(user, input.Role);
            audit.Add("user.create", "user", user.Id, new { user.UserName, input.Role, input.MaxDiscountPercent });
            await db.SaveChangesAsync();
            return Results.Ok(new { user.Id });
        });

        g.MapPut("/{id:guid}", async (Guid id, UserUpdateInput input, UserManager<AppUser> users, HttpContext http,
            AuditWriter audit, AppDbContext db) =>
        {
            ValidateRole(input.Role);
            ValidateDiscount(input.MaxDiscountPercent);
            var user = await users.FindByIdAsync(id.ToString()) ?? throw new NotFoundException("Kullanıcı");
            var self = users.GetUserId(http.User) == user.Id.ToString();
            if (self && (!input.IsActive || input.Role != Roles.Yonetici))
                throw new DomainException("self", "Kendi yönetici yetkinizi veya hesabınızı kapatamazsınız.");

            user.FullName = TextRules.CleanName(input.FullName);
            user.MaxDiscountPercent = input.MaxDiscountPercent;
            var wasActive = user.IsActive;
            user.IsActive = input.IsActive;
            await users.UpdateAsync(user);
            var current = await users.GetRolesAsync(user);
            if (!current.SequenceEqual([input.Role]))
            {
                await users.RemoveFromRolesAsync(user, current);
                await users.AddToRoleAsync(user, input.Role);
            }
            // Personel ayrilisinda tek islemle tum erisim kapanir (standart 2.2).
            if (wasActive && !input.IsActive) await users.UpdateSecurityStampAsync(user);
            audit.Add("user.update", "user", user.Id, new { input.Role, input.MaxDiscountPercent, input.IsActive });
            await db.SaveChangesAsync();
            return Results.Ok(new { ok = true });
        });

        g.MapPost("/{id:guid}/password", async (Guid id, PasswordInput input, UserManager<AppUser> users,
            AuditWriter audit, AppDbContext db) =>
        {
            var user = await users.FindByIdAsync(id.ToString()) ?? throw new NotFoundException("Kullanıcı");
            var token = await users.GeneratePasswordResetTokenAsync(user);
            var result = await users.ResetPasswordAsync(user, token, input.Password);
            if (!result.Succeeded) throw new DomainException("password", AuthEndpoints.Describe(result));
            await users.SetLockoutEndDateAsync(user, null);
            audit.Add("user.password_reset", "user", user.Id);
            await db.SaveChangesAsync();
            return Results.Ok(new { ok = true });
        });

        g.MapPost("/{id:guid}/reset-mfa", async (Guid id, UserManager<AppUser> users, AuditWriter audit, AppDbContext db) =>
        {
            var user = await users.FindByIdAsync(id.ToString()) ?? throw new NotFoundException("Kullanıcı");
            await users.SetTwoFactorEnabledAsync(user, false);
            await users.ResetAuthenticatorKeyAsync(user);
            await users.UpdateSecurityStampAsync(user);
            audit.Add("user.mfa_reset", "user", user.Id);
            await db.SaveChangesAsync();
            return Results.Ok(new { ok = true });
        });
    }

    private static void ValidateRole(string role)
    {
        if (!Roles.All.Contains(role)) throw new DomainException("role", "Geçersiz rol.");
    }

    private static void ValidateDiscount(decimal value)
    {
        if (value is < 0 or > 100) throw new DomainException("discount", "İskonto sınırı 0 ile 100 arasında olmalı.");
    }
}
