using System.Globalization;
using System.Text;

namespace UUSport.Api.Infrastructure;

/// <summary>
/// ADR-003: Turkce metin kurallari. Insan metni Turkce kurallarla, kod ve anahtarlar
/// ASCII (invariant) kurallarla islenir. Aramalar tek bir normalizasyondan gecer.
/// </summary>
public static class TextRules
{
    public static readonly CultureInfo Turkish = CultureInfo.GetCultureInfo("tr-TR");

    /// <summary>"IŞIK", "Işık", "isik" ayni anahtari uretir.</summary>
    public static string SearchKey(params string?[] parts)
    {
        var joined = string.Join(' ', parts.Where(p => !string.IsNullOrWhiteSpace(p)));
        var lower = joined.ToLower(Turkish);
        var sb = new StringBuilder(lower.Length);
        var lastWasSpace = true;
        foreach (var ch in lower)
        {
            var mapped = ch switch
            {
                'ı' => 'i',
                'ğ' => 'g',
                'ü' => 'u',
                'ş' => 's',
                'ö' => 'o',
                'ç' => 'c',
                'â' => 'a',
                'î' => 'i',
                'û' => 'u',
                _ => ch,
            };
            if (char.IsWhiteSpace(mapped))
            {
                if (!lastWasSpace) sb.Append(' ');
                lastWasSpace = true;
                continue;
            }
            if (char.IsLetterOrDigit(mapped) || mapped is '@' or '.' or '-' or '_')
            {
                sb.Append(mapped);
                lastWasSpace = false;
            }
        }
        return sb.ToString().Trim();
    }

    /// <summary>E-posta ve kullanici adi ASCII kurallariyla kucuk harfe cevrilir.</summary>
    public static string? NormalizeEmail(string? email) =>
        string.IsNullOrWhiteSpace(email) ? null : email.Trim().ToLowerInvariant();

    /// <summary>Telefon: yalniz rakamlar; 0 ile baslayan 11 haneli numara 10 haneye indirilir.</summary>
    public static string? NormalizePhone(string? phone)
    {
        if (string.IsNullOrWhiteSpace(phone)) return null;
        var digits = new string(phone.Where(char.IsAsciiDigit).ToArray());
        if (digits.StartsWith("90", StringComparison.Ordinal) && digits.Length == 12) digits = digits[2..];
        if (digits.StartsWith('0') && digits.Length == 11) digits = digits[1..];
        return digits.Length == 0 ? null : digits;
    }

    public static string CleanName(string? value) =>
        string.Join(' ', (value ?? string.Empty).Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));

    /// <summary>
    /// Metinden para/sayi okur. Once invariant ("1234.56"), sonra Turkce ("1.234,56") dener.
    /// Excel'de hucre bicimi degil ham deger kullanilmalidir (ENS tuzagi: "0.##" -> "500,").
    /// </summary>
    public static bool TryParseDecimal(string? text, out decimal value)
    {
        value = 0;
        if (string.IsNullOrWhiteSpace(text)) return false;
        var t = text.Trim().Replace("TL", "", StringComparison.OrdinalIgnoreCase).Replace("₺", "", StringComparison.Ordinal).Trim();
        if (t.Contains(',', StringComparison.Ordinal))
            return decimal.TryParse(t, NumberStyles.Number, Turkish, out value);
        return decimal.TryParse(t, NumberStyles.Number, CultureInfo.InvariantCulture, out value);
    }
}
