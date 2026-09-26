using System.Runtime.Versioning;
using System.Security.Cryptography;
using System.Text;
using Npgsql;

namespace UUSport.Api.Infrastructure;

/// <summary>
/// PowerShell ConvertFrom-SecureString ile yazilmis DPAPI (CurrentUser) dosyalarini okur.
/// Parolalar koda, ayar dosyasina ve komut satirina yazilmaz (guvenlik standardi bolum 5).
/// </summary>
[SupportedOSPlatform("windows")]
public static class DpapiSecrets
{
    public static string SecretDirectory =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "UUSport", "secrets");

    public static string Read(string fileName)
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

    [SupportedOSPlatform("windows")]
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
