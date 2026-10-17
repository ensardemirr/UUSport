using System.Data.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using UUSport.Api.Infrastructure;

namespace UUSport.Api.Data;

/// <summary>
/// Istek boyunca gecerli sirket baglami (ADR-012 2.3). <see cref="ReadTenantIds"/> okunabilen sirket kumesi
/// (birlesik gorunum), <see cref="TenantId"/> yazilabilen tek sirkettir. RLS politikalari ikisini de okur.
/// </summary>
public sealed class TenantContext
{
    private IReadOnlyList<Guid>? _readTenantIds;

    /// <summary>Yazma sirketi. Birden fazla sirket seciliyken bostur.</summary>
    public Guid? TenantId { get; set; }

    /// <summary>Okuma kumesi. Acikca verilmediyse yazma sirketinden turer.</summary>
    public IReadOnlyList<Guid> ReadTenantIds
    {
        get => _readTenantIds ?? (TenantId is { } t ? [t] : []);
        set => _readTenantIds = value;
    }

    /// <summary>Yazma sirketinin kisa kodu (kod on eki icin). Ara katman doldurur; bossa veritabanindan okunur.</summary>
    public string? TenantCode { get; set; }

    /// <summary>Birden fazla sirket secili (birlesik gorunum): yalniz okuma.</summary>
    public bool IsConsolidated => _readTenantIds is { Count: > 1 };

    public Guid Require()
    {
        if (TenantId is { } id) return id;
        if (IsConsolidated) throw new DomainException("company", "Kayıt için tek şirket seçin.");
        throw new InvalidOperationException("Kiraci baglami ayarlanmadi; kurulum tamamlanmamis olabilir.");
    }

    /// <summary>Yazma sirketinin kodunu doner; ara katman doldurmadiysa veritabanindan okur ve saklar.</summary>
    public async Task<string> RequireCodeAsync(AppDbContext db, CancellationToken ct)
    {
        if (!string.IsNullOrEmpty(TenantCode)) return TenantCode;
        var id = Require();
        TenantCode = await db.Tenants.Where(t => t.Id == id).Select(t => t.Code).SingleAsync(ct);
        return TenantCode;
    }
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
        cmd.CommandText = "SELECT set_config('app.tenant_id', @t, false), set_config('app.tenant_ids', @r, false)";
        var p = cmd.CreateParameter();
        p.ParameterName = "t";
        p.Value = tenant.TenantId?.ToString() ?? string.Empty;
        cmd.Parameters.Add(p);
        var r = cmd.CreateParameter();
        r.ParameterName = "r";
        r.Value = string.Join(',', tenant.ReadTenantIds);
        cmd.Parameters.Add(r);
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
