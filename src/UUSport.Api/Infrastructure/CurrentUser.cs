using System.Security.Claims;
using UUSport.Api.Domain;

namespace UUSport.Api.Infrastructure;

public interface ICurrentUser
{
    Guid? UserId { get; }
    string? UserName { get; }
    bool IsInRole(string role);
    /// <summary>Iskonto siniri; kullanici kaydindan okunur.</summary>
    Task<decimal> GetMaxDiscountPercentAsync(CancellationToken ct);
}

public sealed class HttpCurrentUser(IHttpContextAccessor http, Microsoft.AspNetCore.Identity.UserManager<AppUser> users,
    Features.Auth.CompanyAccess access) : ICurrentUser
{
    private ClaimsPrincipal? Principal => http.HttpContext?.User;

    public Guid? UserId =>
        Guid.TryParse(Principal?.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : null;

    public string? UserName => Principal?.Identity?.Name;

    /// <summary>Rol sirket basinadir (ADR-012): secili yazma sirketindeki uyelikten okunur, Identity claim'inden degil.</summary>
    public bool IsInRole(string role) =>
        access.Resolved && access.Selected.Count > 0 &&
        (access.IsGroupAdmin || access.Selected.All(id => access.GrantFor(id)?.Role == role));

    public async Task<decimal> GetMaxDiscountPercentAsync(CancellationToken ct)
    {
        if (UserId is null) return 0;
        var user = await users.FindByIdAsync(UserId.Value.ToString());
        return user?.MaxDiscountPercent ?? 0;
    }
}
