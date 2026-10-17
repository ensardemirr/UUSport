using System.Runtime.Versioning;
using System.Security.Cryptography;
using System.Text;
using Npgsql;

namespace UUSport.Api.Infrastructure;

/// <summary>
/// PowerShell ConvertFrom-SecureString ile yazilmis DPAPI (CurrentUser) dosyalarini okur.
/// Parolalar koda, ayar dosyasina ve komut satirina yazilmaz (guvenlik standardi bolum 5).
/// </summary>
public static class DpapiSecrets
{
    public static string SecretDirectory =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "UUSport", "secrets");

    /// <summary>
    /// Gizli bilgi once ortam degiskeninden okunur: dosya adi buyuk harfe cevrilir, nokta ve tire alt
    /// cizgi olur, basina UUSPORT_SECRET_ gelir (orn. uusport-db-app.dpapi -> UUSPORT_SECRET_UUSPORT_DB_APP_DPAPI).
    /// Bu yol Windows disi CI/test kosulari icindir (DPAPI yoktur); canli Windows kurulumunda ortam
    /// degiskeni tanimlanmaz ve DPAPI dosyasi kullanilir. Parola yine koda/ayar dosyasina yazilmaz.
    /// </summary>
    public static string EnvironmentVariableName(string fileName) =>
        "UUSPORT_SECRET_" + fileName.ToUpperInvariant().Replace('.', '_').Replace('-', '_');

    public static string Read(string fileName)
    {
        var fromEnv = Environment.GetEnvironmentVariable(EnvironmentVariableName(fileName));
        if (!string.IsNullOrEmpty(fromEnv)) return fromEnv;
        if (!OperatingSystem.IsWindows())
            throw new InvalidOperationException(
                $"Bu platformda DPAPI yok; gizli bilgi icin {EnvironmentVariableName(fileName)} ortam degiskeni gerekir.");
        return ReadDpapi(fileName);
    }

    [SupportedOSPlatform("windows")]
    private static string ReadDpapi(string fileName)
    {
        var path = Path.Combine(SecretDirectory, fileName);
        if (!File.Exists(path)) throw new InvalidOperationException($"Gizli bilgi dosyasi bulunamadi: {fileName}");
        var hex = File.ReadAllText(path).Trim();
        var protectedBytes = Convert.FromHexString(hex);
        var plain = ProtectedData.Unprotect(protectedBytes, null, DataProtectionScope.CurrentUser);
        try
        {
            return Encoding.Unicode.GetString(plain);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(plain);
        }
    }
}

public sealed class DatabaseOptions
{
    public string Host { get; set; } = "127.0.0.1";
    public int Port { get; set; } = 5432;
    public string Database { get; set; } = "uusport";
    public string AppUser { get; set; } = "uusport_app";
    public string AppSecretFile { get; set; } = "uusport-db-app.dpapi";
    public string OwnerUser { get; set; } = "uusport_owner";
    public string OwnerSecretFile { get; set; } = "uusport-db-owner.dpapi";

    public string BuildConnectionString(bool asOwner, string? databaseOverride = null)
    {
        var builder = new NpgsqlConnectionStringBuilder
        {
            Host = Host,
            Port = Port,
            Database = databaseOverride ?? Database,
            Username = asOwner ? OwnerUser : AppUser,
            Password = DpapiSecrets.Read(asOwner ? OwnerSecretFile : AppSecretFile),
            ApplicationName = asOwner ? "uusport-migration" : "uusport-api",
            IncludeErrorDetail = false,
        };
        return builder.ConnectionString;
    }
}
