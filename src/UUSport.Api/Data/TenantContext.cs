using System.Data.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace UUSport.Api.Data;

/// <summary>Istek boyunca gecerli kiraci. RLS politikalari bu degeri okur.</summary>
public sealed class TenantContext
{
    public Guid? TenantId { get; set; }

    public Guid Require() =>
        TenantId ?? throw new InvalidOperationException("Kiraci baglami ayarlanmadi; kurulum tamamlanmamis olabilir.");
}

/// <summary>
/// Her baglanti acilisinda app.tenant_id ayarlanir (PostgreSQL standardi bolum 7).
/// Deger yoksa bos gider; RLS politikasi bu durumda hicbir satir gostermez.
/// </summary>
public sealed class TenantConnectionInterceptor(TenantContext tenant) : DbConnectionInterceptor
{
    public override void ConnectionOpened(DbConnection connection, ConnectionEndEventData eventData)
    {
        using var cmd = Build(connection);
        cmd.ExecuteNonQuery();
    }

    public override async Task ConnectionOpenedAsync(DbConnection connection, ConnectionEndEventData eventData,
        CancellationToken cancellationToken = default)
    {
        await using var cmd = Build(connection);
        await cmd.ExecuteNonQueryAsync(cancellationToken);
    }

    private DbCommand Build(DbConnection connection)
    {
        var cmd = connection.CreateCommand();
        cmd.CommandText = "SELECT set_config('app.tenant_id', @t, false)";
        var p = cmd.CreateParameter();
        p.ParameterName = "t";
        p.Value = tenant.TenantId?.ToString() ?? string.Empty;
        cmd.Parameters.Add(p);
        return cmd;
    }
}

/// <summary>Tek kiracili ilk surumde kiraci kimligini bir kez okuyup onbellekte tutar.</summary>
public sealed class TenantResolver(IServiceScopeFactory scopes)
{
    private Guid? _cached;

    public async Task<Guid?> GetAsync(CancellationToken ct)
    {
        if (_cached is not null) return _cached;
        using var scope = scopes.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var id = await db.Tenants.OrderBy(t => t.CreatedAt).Select(t => (Guid?)t.Id).FirstOrDefaultAsync(ct);
        if (id is not null) _cached = id;
        return id;
    }

    public void Set(Guid id) => _cached = id;
}
