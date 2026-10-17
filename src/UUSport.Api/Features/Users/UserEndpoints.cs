using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using UUSport.Api.Data;
using UUSport.Api.Domain;
using UUSport.Api.Features.Audit;
using UUSport.Api.Features.Auth;
using UUSport.Api.Infrastructure;

namespace UUSport.Api.Features.Users;

/// <summary>Bir sirketteki rol ve ince yetki (ADR-012 2.2, 2.4).</summary>
public sealed record MembershipInput(Guid TenantId, string Role, string[]? GrantedKeys = null, string[]? DeniedKeys = null);

/// <summary>
/// <paramref name="Role"/> eski tek-sirket bicimi: verilirse secili yazma sirketine uyelik olur.
/// <paramref name="Memberships"/> verilirse o liste esastir.
/// </summary>
public sealed record UserCreateInput(string UserName, string FullName, string Password, string? Role,
    decimal MaxDiscountPercent, MembershipInput[]? Memberships = null, bool IsGroupAdmin = false);

public sealed record UserUpdateInput(string FullName, string? Role, decimal MaxDiscountPercent, bool IsActive,
    MembershipInput[]? Memberships = null, bool? IsGroupAdmin = null);

public sealed record PasswordInput(string Password);

/// <summary>
/// Kullanici ve uyelik yonetimi. Grup yoneticisi herkesi gorur ve her sirkete uyelik verir; sirket
/// Yonetici'si yalniz yonettigi sirketlerdeki uyelikleri gorur ve degistirir, grup yoneticiligi veremez.
/// </summary>
public static class UserEndpoints
{
    public static void MapUsers(this IEndpointRouteBuilder app)
    {
        var g = app.MapGroup("/api/users").RequireAuthorization(Policies.Admin);

        g.MapGet("", async (AppDbContext db, CompanyAccess access, CancellationToken ct) =>
        {
            var visibleTenants = VisibleTenants(access);
            var memberships = await db.CompanyMemberships.Where(m => m.IsActive).ToListAsync(ct);
            var tenants = await db.Tenants.ToDictionaryAsync(t => t.Id, t => new { t.Code, t.Name }, ct);
            var query = db.Users.AsQueryable();
            if (!access.IsGroupAdmin)
            {
                var visibleUserIds = memberships.Where(m => visibleTenants.Contains(m.TenantId)).Select(m => m.UserId).Distinct().ToList();
                query = query.Where(u => visibleUserIds.Contains(u.Id));
            }
            var list = await query.OrderBy(u => u.UserName).ToListAsync(ct);
            return Results.Ok(list.Select(u => new
            {
                u.Id, u.UserName, u.FullName, u.MaxDiscountPercent, u.IsActive, u.IsGroupAdmin,
                mfaEnabled = u.TwoFactorEnabled, locked = u.LockoutEnd > DateTimeOffset.UtcNow,
                memberships = memberships.Where(m => m.UserId == u.Id && (access.IsGroupAdmin || visibleTenants.Contains(m.TenantId)))
                    .Select(m => new
                    {
                        m.TenantId,
                        code = tenants.GetValueOrDefault(m.TenantId)?.Code,
                        name = tenants.GetValueOrDefault(m.TenantId)?.Name,
                        m.Role, m.GrantedKeys, m.DeniedKeys,
                        effective = Permissions.Effective(m.Role, m.GrantedKeys, m.DeniedKeys).OrderBy(k => k),
                    }),
            }));
        });

        g.MapGet("/permission-keys", () => Results.Ok(new
        {
            all = Permissions.All,
            grantable = Permissions.Grantable,
            templates = Roles.All.ToDictionary(r => r, r => Permissions.Template(r).OrderBy(k => k).ToArray()),
        }));

        g.MapPost("", async (UserCreateInput input, UserManager<AppUser> users, TenantContext tenant, CompanyAccess access,
            ICurrentUser me, AuditWriter audit, AppDbContext db, CancellationToken ct) =>
        {
            ValidateDiscount(input.MaxDiscountPercent);
            var wanted = ResolveMemberships(input.Memberships, input.Role, tenant, access);
            if (input.IsGroupAdmin && !access.IsGroupAdmin)
                throw new DomainException("group_admin", "Grup yöneticiliğini yalnız grup yöneticisi verebilir.");
            if (wanted.Count == 0 && !input.IsGroupAdmin)
                throw new DomainException("memberships", "Kullanıcıya en az bir şirkette rol verin.");

            var user = new AppUser
            {
                UserName = input.UserName.Trim().ToLowerInvariant(),
                FullName = TextRules.CleanName(input.FullName),
                TenantId = wanted.Count > 0 ? wanted[0].TenantId : tenant.Require(),
                MaxDiscountPercent = input.MaxDiscountPercent,
                IsGroupAdmin = input.IsGroupAdmin,
            };
            if (user.FullName.Length == 0) throw new DomainException("full_name", "Ad soyad zorunlu.");
            var result = await users.CreateAsync(user, input.Password);
            if (!result.Succeeded) throw new DomainException("user", AuthEndpoints.Describe(result));

            foreach (var m in wanted)
            {
                db.CompanyMemberships.Add(new CompanyMembership
                {
                    UserId = user.Id, TenantId = m.TenantId, Role = m.Role,
                    GrantedKeys = m.GrantedKeys ?? [], DeniedKeys = m.DeniedKeys ?? [], GrantedByUserId = me.UserId,
                });
                audit.Add("membership.grant", "user", user.Id, new { m.TenantId, m.Role, m.GrantedKeys, m.DeniedKeys }, tenantOverride: m.TenantId);
            }
            audit.Add("user.create", "user", user.Id, new { user.UserName, input.MaxDiscountPercent, user.IsGroupAdmin },
                tenantOverride: user.TenantId);
            await db.SaveChangesAsync(ct);
            return Results.Ok(new { user.Id });
        });

        g.MapPut("/{id:guid}", async (Guid id, UserUpdateInput input, UserManager<AppUser> users, HttpContext http,
            TenantContext tenant, CompanyAccess access, ICurrentUser me, AuditWriter audit, AppDbContext db, CancellationToken ct) =>
        {
            ValidateDiscount(input.MaxDiscountPercent);
            var user = await FindVisibleAsync(users, db, access, id, ct);
            var self = users.GetUserId(http.User) == user.Id.ToString();
            if (self && !input.IsActive)
                throw new DomainException("self", "Kendi hesabınızı kapatamazsınız.");

            user.FullName = TextRules.CleanName(input.FullName);
            user.MaxDiscountPercent = input.MaxDiscountPercent;
            var wasActive = user.IsActive;
            user.IsActive = input.IsActive;

            if (input.IsGroupAdmin is { } ga && ga != user.IsGroupAdmin)
            {
                if (!access.IsGroupAdmin) throw new DomainException("group_admin", "Grup yöneticiliğini yalnız grup yöneticisi değiştirebilir.");
                if (!ga && self) throw new DomainException("self", "Kendi grup yöneticiliğinizi kaldıramazsınız.");
                if (!ga && await db.Users.CountAsync(u => u.IsGroupAdmin && u.IsActive && u.Id != user.Id, ct) == 0)
                    throw new DomainException("last_group_admin", "Son grup yöneticisi kaldırılamaz.");
                user.IsGroupAdmin = ga;
            }

            var membershipsChanged = false;
            if (input.Memberships is not null || input.Role is not null)
            {
                var wanted = ResolveMemberships(input.Memberships, input.Role, tenant, access);
                var manageable = VisibleTenants(access);
                var current = await db.CompanyMemberships.Where(m => m.UserId == user.Id && m.IsActive).ToListAsync(ct);
                // Yalniz yonetilen sirketlerdeki uyelikler degistirilir; digerleri dokunulmadan kalir.
                foreach (var cur in current.Where(c => access.IsGroupAdmin || manageable.Contains(c.TenantId)))
                {
                    var w = wanted.FirstOrDefault(x => x.TenantId == cur.TenantId);
                    if (w is null)
                    {
                        if (self && cur.Role == Roles.Yonetici && !user.IsGroupAdmin)
                            throw new DomainException("self", "Kendi yönetici yetkinizi kaldıramazsınız.");
                        cur.IsActive = false;
                        cur.RevokedAt = DateTimeOffset.UtcNow;
                        audit.Add("membership.revoke", "user", user.Id, new { cur.TenantId, cur.Role }, tenantOverride: cur.TenantId);
                        membershipsChanged = true;
                    }
                    else if (w.Role != cur.Role || !SameKeys(w.GrantedKeys, cur.GrantedKeys) || !SameKeys(w.DeniedKeys, cur.DeniedKeys))
                    {
                        if (self && cur.Role == Roles.Yonetici && w.Role != Roles.Yonetici && !user.IsGroupAdmin)
                            throw new DomainException("self", "Kendi yönetici yetkinizi düşüremezsiniz.");
                        cur.Role = w.Role;
                        cur.GrantedKeys = w.GrantedKeys ?? [];
                        cur.DeniedKeys = w.DeniedKeys ?? [];
                        audit.Add("membership.update", "user", user.Id, new { cur.TenantId, cur.Role, cur.GrantedKeys, cur.DeniedKeys }, tenantOverride: cur.TenantId);
                        membershipsChanged = true;
                    }
                }
                foreach (var w in wanted.Where(w => current.All(c => c.TenantId != w.TenantId)))
                {
                    db.CompanyMemberships.Add(new CompanyMembership
                    {
                        UserId = user.Id, TenantId = w.TenantId, Role = w.Role,
                        GrantedKeys = w.GrantedKeys ?? [], DeniedKeys = w.DeniedKeys ?? [], GrantedByUserId = me.UserId,
                    });
                    audit.Add("membership.grant", "user", user.Id, new { w.TenantId, w.Role, w.GrantedKeys, w.DeniedKeys }, tenantOverride: w.TenantId);
                    membershipsChanged = true;
                }
            }

            await users.UpdateAsync(user);
            // Personel ayrilisinda veya yetki degisikliginde acik oturumlar en gec 1 dakikada yenilenir (standart 2.2).
            if ((wasActive && !input.IsActive) || membershipsChanged) await users.UpdateSecurityStampAsync(user);
            audit.Add("user.update", "user", user.Id, new { input.MaxDiscountPercent, input.IsActive, user.IsGroupAdmin },
                tenantOverride: user.TenantId);
            await db.SaveChangesAsync(ct);
            return Results.Ok(new { ok = true });
        });

        g.MapPost("/{id:guid}/password", async (Guid id, PasswordInput input, UserManager<AppUser> users, CompanyAccess access,
            AuditWriter audit, AppDbContext db, CancellationToken ct) =>
        {
            var user = await FindVisibleAsync(users, db, access, id, ct);
            var token = await users.GeneratePasswordResetTokenAsync(user);
            var result = await users.ResetPasswordAsync(user, token, input.Password);
            if (!result.Succeeded) throw new DomainException("password", AuthEndpoints.Describe(result));
            await users.SetLockoutEndDateAsync(user, null);
            audit.Add("user.password_reset", "user", user.Id, tenantOverride: user.TenantId);
            await db.SaveChangesAsync(ct);
            return Results.Ok(new { ok = true });
        });

        g.MapPost("/{id:guid}/reset-mfa", async (Guid id, UserManager<AppUser> users, CompanyAccess access,
            AuditWriter audit, AppDbContext db, CancellationToken ct) =>
        {
            var user = await FindVisibleAsync(users, db, access, id, ct);
            await users.SetTwoFactorEnabledAsync(user, false);
            await users.ResetAuthenticatorKeyAsync(user);
            await users.UpdateSecurityStampAsync(user);
            audit.Add("user.mfa_reset", "user", user.Id, tenantOverride: user.TenantId);
            await db.SaveChangesAsync(ct);
            return Results.Ok(new { ok = true });
        });
    }

    /// <summary>Yoneticinin yonettigi sirketler: grup yoneticisi hepsini, sirket Yonetici'si yalniz Yonetici oldugu secilileri.</summary>
    private static HashSet<Guid> VisibleTenants(CompanyAccess access) =>
        access.IsGroupAdmin
            ? access.Allowed.Select(a => a.TenantId).ToHashSet()
            : access.Allowed.Where(a => a.Role == Roles.Yonetici).Select(a => a.TenantId).ToHashSet();

    /// <summary>Baska sirketin kullanicisi "yok" gibi davranir (404), varligi sizmaz.</summary>
    private static async Task<AppUser> FindVisibleAsync(UserManager<AppUser> users, AppDbContext db, CompanyAccess access,
        Guid id, CancellationToken ct)
    {
        var user = await users.FindByIdAsync(id.ToString()) ?? throw new NotFoundException("Kullanıcı");
        if (access.IsGroupAdmin) return user;
        if (user.IsGroupAdmin) throw new NotFoundException("Kullanıcı");
        var visible = VisibleTenants(access);
        var member = await db.CompanyMemberships.AnyAsync(m => m.UserId == id && m.IsActive && visible.Contains(m.TenantId), ct);
        return member ? user : throw new NotFoundException("Kullanıcı");
    }

    private static List<MembershipInput> ResolveMemberships(MembershipInput[]? memberships, string? legacyRole,
        TenantContext tenant, CompanyAccess access)
    {
        var list = memberships?.ToList() ?? [];
        if (list.Count == 0 && !string.IsNullOrWhiteSpace(legacyRole))
            list.Add(new MembershipInput(tenant.Require(), legacyRole));
        var manageable = VisibleTenants(access);
        foreach (var m in list)
        {
            if (!Roles.All.Contains(m.Role)) throw new DomainException("role", "Geçersiz rol.");
            if (!access.IsGroupAdmin && !manageable.Contains(m.TenantId))
                throw new DomainException("tenant", "Yalnız yönettiğiniz şirketlerde rol verebilirsiniz.");
            Permissions.ValidateKeys(m.GrantedKeys ?? [], "granted_keys");
            Permissions.ValidateKeys(m.DeniedKeys ?? [], "denied_keys");
        }
        if (list.Select(m => m.TenantId).Distinct().Count() != list.Count)
            throw new DomainException("memberships", "Aynı şirket için birden fazla rol verilemez.");
        return list;
    }

    private static bool SameKeys(string[]? a, string[] b) =>
        (a ?? []).OrderBy(x => x).SequenceEqual(b.OrderBy(x => x));

    private static void ValidateDiscount(decimal value)
    {
        if (value is < 0 or > 100) throw new DomainException("discount", "İskonto sınırı 0 ile 100 arasında olmalı.");
    }
}
