using System.Text.Json;
using Microsoft.EntityFrameworkCore;
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

public static class AuditSaveExtensions
{
    /// <summary>
    /// RLS yazma politikasi tek sirkettir (ADR-012 2.3). Kayit, yazma sirketinden farkli bir sirkete ait denetim
    /// olayi iceriyorsa (ornegin grup yoneticisinin baska sirkette uyelik vermesi) o olaylar ayni islem
    /// (transaction) icinde, yazma baglami hedef sirkete cevrilerek yazilir; is kaydi ve denetim yine birlikte
    /// kaydedilir veya birlikte geri alinir. <paramref name="saveMain"/> varsayilan olarak SaveChanges'tir;
    /// UserManager gibi bekleyen degisiklikleri kendi kaydeden cagrilar icin verilir. Tum denetim olaylari
    /// bu cagridan ONCE eklenmis olmalidir.
    /// </summary>
    public static async Task SaveChangesAcrossCompaniesAsync(this AppDbContext db, TenantContext tenant, CancellationToken ct,
        Func<Task>? saveMain = null)
    {
        saveMain ??= () => db.SaveChangesAsync(ct);
        var foreign = db.ChangeTracker.Entries<AuditEvent>()
            .Where(e => e.State == EntityState.Added && e.Entity.TenantId != tenant.TenantId).ToList();
        if (foreign.Count == 0)
        {
            await saveMain();
            return;
        }

        foreach (var e in foreign) e.State = EntityState.Detached;
        var (writeTenant, readTenants) = (tenant.TenantId, tenant.ReadTenantIds);
        await using var tx = await db.Database.BeginTransactionAsync(ct);
        try
        {
            await saveMain();
            foreach (var group in foreign.GroupBy(e => e.Entity.TenantId))
            {
                var id = group.Key.ToString();
                await db.Database.ExecuteSqlInterpolatedAsync(
                    $"SELECT set_config('app.tenant_id', {id}, false), set_config('app.tenant_ids', {id}, false)", ct);
                tenant.TenantId = group.Key;
                tenant.ReadTenantIds = [group.Key];
                db.AuditEvents.AddRange(group.Select(e => e.Entity));
                await db.SaveChangesAsync(ct);
            }
            await tx.CommitAsync(ct);
        }
        finally
        {
            tenant.TenantId = writeTenant;
            tenant.ReadTenantIds = readTenants;
        }
    }
}
