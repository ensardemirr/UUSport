using System.Text.Json;
using UUSport.Api.Data;
using UUSport.Api.Domain;
using UUSport.Api.Infrastructure;

namespace UUSport.Api.Features.Audit;

/// <summary>
/// Denetim olayini is kaydiyla ayni SaveChanges icinde ekler; biri yazilmadan digeri yazilmaz.
/// Parola, token ve gereksiz kisisel veri ozet icine konmaz (standart bolum 8).
/// </summary>
public sealed class AuditWriter(AppDbContext db, TenantContext tenant, ICurrentUser user, IHttpContextAccessor http)
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        Converters = { new DecimalAsStringConverter(), new NullableDecimalAsStringConverter() },
    };

    public void Add(string action, string targetType, Guid? targetId, object? summary = null, Guid? tenantOverride = null)
    {
        db.AuditEvents.Add(new AuditEvent
        {
            TenantId = tenantOverride ?? tenant.Require(),
            UserId = user.UserId,
            UserName = user.UserName,
            Action = action,
            TargetType = targetType,
            TargetId = targetId,
            CorrelationId = http.HttpContext?.TraceIdentifier ?? Guid.NewGuid().ToString("N"),
            Summary = summary is null ? null : JsonSerializer.Serialize(summary, Json),
        });
    }
}
