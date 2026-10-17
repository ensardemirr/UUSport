using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using UUSport.Api.Data;
using UUSport.Api.Domain;

namespace UUSport.Api.Features.Auth;

/// <summary>Kullanicinin bir sirketteki etkin rolu ve yetki anahtarlari.</summary>
public sealed record CompanyGrant(Guid TenantId, string Code, string Name, string Role, IReadOnlySet<string> Permissions,
    bool IsActive);

/// <summary>
/// Istek boyunca gecerli sirket erisimi (ADR-012 2.2-2.4). <see cref="CompanyContextMiddleware"/> doldurur;
/// yetki politikalari ve uclar buradan okur. Grup yoneticisi her aktif sirkette Yonetici sayilir.
/// </summary>
public sealed class CompanyAccess
{
    public const string Header = "X-UUSport-Companies";

    public bool Resolved { get; private set; }
    public bool IsGroupAdmin { get; private set; }
    public IReadOnlyList<CompanyGrant> Allowed { get; private set; } = [];
    public IReadOnlyList<Guid> Selected { get; private set; } = [];

    public void Set(bool isGroupAdmin, IReadOnlyList<CompanyGrant> allowed, IReadOnlyList<Guid> selected)
    {
        IsGroupAdmin = isGroupAdmin;
        Allowed = allowed;
        Selected = selected;
        Resolved = true;
    }

    public CompanyGrant? GrantFor(Guid tenantId) => Allowed.FirstOrDefault(a => a.TenantId == tenantId);

    public bool CanRead(Guid tenantId) => GrantFor(tenantId) is not null;

    /// <summary>Secili her sirkette anahtar var mi? Secim bossa false.</summary>
    public bool HasInAllSelected(string key) =>
        Selected.Count > 0 && Selected.All(id => GrantFor(id)?.Permissions.Contains(key) == true);

    /// <summary>Secili her sirkette Yonetici (veya grup yoneticisi).</summary>
    public bool IsAdminInAllSelected() =>
        IsGroupAdmin || (Selected.Count > 0 && Selected.All(id => GrantFor(id)?.Role == Roles.Yonetici));

    public bool IsAdminIn(Guid tenantId) => IsGroupAdmin || GrantFor(tenantId)?.Role == Roles.Yonetici;

    /// <summary>Kullanicinin uyeliklerinden erisim listesi uretir (grup yoneticisi: tum aktif sirketler).</summary>
    public static async Task<(bool IsGroupAdmin, List<CompanyGrant> Allowed)> LoadAsync(AppDbContext db, AppUser user,
        CancellationToken ct)
    {
        var tenants = await db.Tenants.Where(t => t.IsActive).OrderBy(t => t.SortOrder).ThenBy(t => t.CreatedAt)
            .Select(t => new { t.Id, t.Code, t.Name }).ToListAsync(ct);
        if (user.IsGroupAdmin)
            return (true, tenants.Select(t => new CompanyGrant(t.Id, t.Code, t.Name, Roles.Yonetici,
                Permissions.Template(Roles.Yonetici), true)).ToList());

        var memberships = await db.CompanyMemberships
            .Where(m => m.UserId == user.Id && m.IsActive).ToListAsync(ct);
        var list = new List<CompanyGrant>();
        foreach (var t in tenants)
        {
            var m = memberships.FirstOrDefault(x => x.TenantId == t.Id);
            if (m is null) continue;
            list.Add(new CompanyGrant(t.Id, t.Code, t.Name, m.Role, Permissions.Effective(m.Role, m.GrantedKeys, m.DeniedKeys), true));
        }
        return (false, list);
    }
}

/// <summary>
/// Sirket baglami ara katmani (ADR-012 2.3). UseAuthentication'dan SONRA calisir: oturum acmis kullanicinin
/// uyelikleri okunur, istemcinin `X-UUSport-Companies` basligi bu kumeyle KESISTIRILIR (bos kesisim 403),
/// okuma kumesi ve (tek sirketse) yazma sirketi TenantContext'e yazilir. Yazan isteklerde Ortak rolu 403 alir.
/// Oturumsuz isteklerde (kurulum, giris) ilk kiraci kullanilir; TenantResolver yalniz bu amacla kalir.
/// </summary>
public sealed class CompanyContextMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext ctx, TenantContext tenant, CompanyAccess access, AppDbContext db,
        TenantResolver resolver)
    {
        if (!ctx.Request.Path.StartsWithSegments("/api"))
        {
            await next(ctx);
            return;
        }

        var userId = Guid.TryParse(ctx.User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : (Guid?)null;
        AppUser? user = null;
        if (ctx.User.Identity?.IsAuthenticated == true && userId is not null)
            user = await db.Users.AsNoTracking().SingleOrDefaultAsync(u => u.Id == userId, ctx.RequestAborted);

        if (user is null || !user.IsActive)
        {
            tenant.TenantId = await resolver.GetAsync(ctx.RequestAborted);
            await next(ctx);
            return;
        }

        var (isGroupAdmin, allowed) = await CompanyAccess.LoadAsync(db, user, ctx.RequestAborted);
        var requested = ParseHeader(ctx.Request.Headers[CompanyAccess.Header]);
        List<Guid> selected;
        if (requested is null)
        {
            // Baslik yok: ev sirketi, yoksa ilk izinli sirket.
            selected = allowed.Any(a => a.TenantId == user.TenantId) ? [user.TenantId]
                : allowed.Count > 0 ? [allowed[0].TenantId] : [];
        }
        else if (requested.Count == 0)
        {
            // "all": izinli her sirket.
            selected = allowed.Select(a => a.TenantId).ToList();
        }
        else
        {
            selected = requested.Where(r => allowed.Any(a => a.TenantId == r)).Distinct().ToList();
            if (selected.Count == 0)
            {
                await Forbid(ctx, "company", "Seçilen şirket için yetkiniz yok.");
                return;
            }
        }

        access.Set(isGroupAdmin, allowed, selected);
        tenant.ReadTenantIds = selected;
        tenant.TenantId = selected.Count == 1 ? selected[0] : null;
        tenant.TenantCode = selected.Count == 1 ? access.GrantFor(selected[0])?.Code : null;

        var isWrite = !HttpMethods.IsGet(ctx.Request.Method) && !HttpMethods.IsHead(ctx.Request.Method)
            && !HttpMethods.IsOptions(ctx.Request.Method);
        // Oturum/MFA uclari sirket yetkisi gerektirmez; is uclari gerektirir.
        var isAccountPath = ctx.Request.Path.StartsWithSegments("/api/auth") || ctx.Request.Path.StartsWithSegments("/api/setup");
        if (isWrite && !isAccountPath && selected.Count == 1 && !access.HasInAllSelected(Permissions.Write))
        {
            await Forbid(ctx, "read_only_role", "Bu şirkette rolünüz salt okunur; kayıt yapamazsınız.");
            return;
        }

        await next(ctx);
    }

    /// <summary>null: baslik yok. Bos liste: "all". Aksi halde istenen kimlikler.</summary>
    private static List<Guid>? ParseHeader(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return null;
        if (raw.Trim().Equals("all", StringComparison.OrdinalIgnoreCase)) return [];
        var list = new List<Guid>();
        foreach (var part in raw.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            if (Guid.TryParse(part, out var g)) list.Add(g);
        return list;
    }

    private static async Task Forbid(HttpContext ctx, string code, string message)
    {
        ctx.Response.StatusCode = StatusCodes.Status403Forbidden;
        ctx.Response.ContentType = "application/json; charset=utf-8";
        await ctx.Response.WriteAsync(JsonSerializer.Serialize(new { code, message }));
    }
}

// ---------------------------------------------------------------------------
// Yetki politikalari: Staff (secili her sirkette uyelik), Admin (secili her sirkette Yonetici veya grup
// yoneticisi), GroupAdmin, perm:<anahtar> (secili her sirkette anahtar).
// ---------------------------------------------------------------------------

public sealed class CompanyRequirement(CompanyRequirement.Level level) : IAuthorizationRequirement
{
    public enum Level { Member, Admin, GroupAdmin }
    public Level Required { get; } = level;
}

public sealed class PermissionRequirement(string key) : IAuthorizationRequirement
{
    public string Key { get; } = key;
}

public sealed class CompanyAuthorizationHandler(CompanyAccess access) : IAuthorizationHandler
{
    public Task HandleAsync(AuthorizationHandlerContext context)
    {
        if (context.User.Identity?.IsAuthenticated != true || !access.Resolved) return Task.CompletedTask;
        foreach (var req in context.PendingRequirements.ToList())
        {
            switch (req)
            {
                case CompanyRequirement c when c.Required switch
                {
                    CompanyRequirement.Level.Member => access.Selected.Count > 0,
                    CompanyRequirement.Level.Admin => access.IsAdminInAllSelected(),
                    CompanyRequirement.Level.GroupAdmin => access.IsGroupAdmin,
                    _ => false,
                }:
                    context.Succeed(req);
                    break;
                case PermissionRequirement p when access.HasInAllSelected(p.Key):
                    context.Succeed(req);
                    break;
            }
        }
        return Task.CompletedTask;
    }
}

public static class PermissionEndpointExtensions
{
    /// <summary>Uca/gruba yetki anahtari kosulu ekler; Staff ile birlikte (VE) uygulanir.</summary>
    public static TBuilder RequirePermission<TBuilder>(this TBuilder builder, string key) where TBuilder : IEndpointConventionBuilder =>
        builder.RequireAuthorization(Permissions.PolicyName(key));
}
