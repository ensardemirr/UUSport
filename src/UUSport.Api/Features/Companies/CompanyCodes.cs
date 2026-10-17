using System.Text;
using UUSport.Api.Infrastructure;

namespace UUSport.Api.Features.Companies;

/// <summary>
/// Sirket kisa kodu ve kod on eki kurallari (ADR-012 2.8). Sistem kodlari `UUS-C000001` / `UUS-U000001`
/// bicimindedir; kullanicinin elle verdigi kod on eksiz girilir, sistem on eki ekler.
/// </summary>
public static class CompanyCodes
{
    public const int MinLength = 2;
    public const int MaxLength = 6;

    /// <summary>Kullanici girdisini kisa koda cevirir: ASCII buyuk harf/rakam, 2-6 karakter.</summary>
    public static string Normalize(string? input)
    {
        var raw = Ascii(input ?? string.Empty).ToUpperInvariant();
        var sb = new StringBuilder();
        foreach (var c in raw) if (char.IsAsciiLetterOrDigit(c)) sb.Append(c);
        var code = sb.ToString();
        if (code.Length is < MinLength or > MaxLength)
            throw new DomainException("code", $"Şirket kodu {MinLength}-{MaxLength} harf/rakam olmalı (örn. UUS).");
        if (!char.IsAsciiLetter(code[0]))
            throw new DomainException("code", "Şirket kodu harfle başlamalı.");
        return code;
    }

    /// <summary>Sirket adindan uc harfli kod turetir (UUSPORT -> UUS, Fitness -> FIT).</summary>
    public static string Derive(string name)
    {
        var letters = new string(Ascii(name).ToUpperInvariant().Where(char.IsAsciiLetter).ToArray());
        if (letters.Length == 0) letters = "SRK";
        return letters.Length >= 3 ? letters[..3] : letters.PadRight(3, 'X');
    }

    /// <summary>Verilen cari/urun koduna sirket on ekini ekler; zaten varsa dokunmaz.</summary>
    public static string WithPrefix(string companyCode, string code)
    {
        var trimmed = code.Trim().ToUpperInvariant();
        var prefix = companyCode + "-";
        return trimmed.StartsWith(prefix, StringComparison.Ordinal) ? trimmed : prefix + trimmed;
    }

    private static string Ascii(string s) => s
        .Replace('ı', 'i').Replace('İ', 'I').Replace('ş', 's').Replace('Ş', 'S').Replace('ğ', 'g').Replace('Ğ', 'G')
        .Replace('ü', 'u').Replace('Ü', 'U').Replace('ö', 'o').Replace('Ö', 'O').Replace('ç', 'c').Replace('Ç', 'C');
}
