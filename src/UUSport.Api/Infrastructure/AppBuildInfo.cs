using System.Text.Json;

namespace UUSport.Api.Infrastructure;

/// <summary>
/// Sunulan web arayuzunun derleme kimligi (wwwroot/version.json). API yanitlarina X-App-Build
/// olarak eklenir; acik sekme eski kimlikteyse "yeni surum" bandi gosterir (S-08). Dosya yeniden
/// derlenince sunucu yeniden baslatilmadan da guncellenir (en fazla 5 sn gecikme).
/// </summary>
public sealed class AppBuildInfo(IWebHostEnvironment env)
{
    private readonly Lock _gate = new();
    private string? _value;
    private DateTime _fileStamp;
    private DateTime _checkedAt;

    public string? Current
    {
        get
        {
            lock (_gate)
            {
                var now = DateTime.UtcNow;
                if (now - _checkedAt < TimeSpan.FromSeconds(5)) return _value;
                _checkedAt = now;
                var path = Path.Combine(env.WebRootPath ?? string.Empty, "version.json");
                if (!File.Exists(path)) return _value = null;
                var stamp = File.GetLastWriteTimeUtc(path);
                if (stamp == _fileStamp) return _value;
                _fileStamp = stamp;
                try
                {
                    using var doc = JsonDocument.Parse(File.ReadAllText(path));
                    _value = doc.RootElement.GetProperty("build").GetString();
                }
                catch (Exception ex) when (ex is IOException or JsonException or KeyNotFoundException)
                {
                    _value = null;
                }
                return _value;
            }
        }
    }
}
