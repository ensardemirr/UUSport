using System.Security.Cryptography;
using System.Text;

namespace UUSport.Api.Infrastructure;

/// <summary>
/// Kurulum ucu (guvenlik standardi S-12): ilk yonetici hesabi olusturulmadan once tek
/// kullanimlik bir jeton gerekir. Boylece agdaki/internetteki baskasi, gercek yonetici
/// /api/setup'a ulasmadan once kendi hesabini kurup sistemi ele gecirmez. Jeton yalniz bu
/// makinede, DataProtection anahtarlariyla ayni ACL'li klasorde saklanir ve sunucu logunda
/// bir kez gorunur; kurulum tamamlaninca silinir ve tekrar kullanilamaz.
/// </summary>
public sealed class SetupTokenService(ILogger<SetupTokenService> log)
{
    private readonly string _path = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "UUSport", "secrets", "setup-token.txt");
    private readonly Lock _gate = new();

    public string EnsureToken()
    {
        lock (_gate)
        {
            if (File.Exists(_path)) return File.ReadAllText(_path).Trim();
            var token = Convert.ToHexString(RandomNumberGenerator.GetBytes(24)).ToLowerInvariant();
            Directory.CreateDirectory(Path.GetDirectoryName(_path)!);
            File.WriteAllText(_path, token);
            log.LogWarning(
                "Kurulum jetonu uretildi. Ilk yonetici hesabini kurarken bu jetonu girin (bir kez gecerlidir): {Token}", token);
            return token;
        }
    }

    public bool Validate(string? input)
    {
        lock (_gate)
        {
            if (!File.Exists(_path) || string.IsNullOrEmpty(input)) return false;
            var expected = Encoding.UTF8.GetBytes(File.ReadAllText(_path).Trim());
            return CryptographicOperations.FixedTimeEquals(expected, Encoding.UTF8.GetBytes(input));
        }
    }

    public void Consume()
    {
        lock (_gate)
        {
            if (File.Exists(_path)) File.Delete(_path);
        }
    }
}
