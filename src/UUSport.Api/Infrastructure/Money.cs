namespace UUSport.Api.Infrastructure;

/// <summary>
/// ADR-002: tek yuvarlama noktasi. Yontem daima "yarim yukari" (AwayFromZero);
/// .NET varsayilani olan banker yuvarlamasi PostgreSQL round() ile farkli sonuc verir.
/// </summary>
public static class Money
{
    /// <summary>Kesinlesmis para tutari: 2 hane.</summary>
    public static decimal Round(decimal value) => Math.Round(value, 2, MidpointRounding.AwayFromZero);

    /// <summary>Satir KDV'si ve ara hesaplar: 6 hane (ADR-002 bolum 2.1).</summary>
    public static decimal Round6(decimal value) => Math.Round(value, 6, MidpointRounding.AwayFromZero);

    /// <summary>Miktar: 4 hane.</summary>
    public static decimal RoundQuantity(decimal value) => Math.Round(value, 4, MidpointRounding.AwayFromZero);

    public static bool HasAtMostTwoDecimals(decimal value) => Round(value) == value;

    /// <summary>
    /// Satir tutari hesabi (ADR-002 bolum 2.3). KDV satirda 2 haneye yuvarlanmaz.
    /// KDV dahil fiyatta brut tutar kesin kabul edilir; haric fiyatta matrah kesindir.
    /// </summary>
    public static LineAmounts PriceLine(decimal quantity, decimal unitPrice, bool priceIncludesVat,
        decimal discountPercent, decimal vatRate)
    {
        var baseAmount = quantity * unitPrice * (1m - discountPercent / 100m);
        if (priceIncludesVat)
        {
            var gross = Round(baseAmount);
            var net = Round6(gross / (1m + vatRate / 100m));
            return new LineAmounts(net, gross - net, gross);
        }

        var netAmount = Round(baseAmount);
        var vat = Round6(netAmount * vatRate / 100m);
        return new LineAmounts(netAmount, vat, Round(netAmount + vat));
    }

    /// <summary>
    /// Tutari parcalara boler; kurus farki son parcaya eklenir (ADR-002 bolum 2.4).
    /// </summary>
    public static IReadOnlyList<decimal> Split(decimal total, int parts)
    {
        if (parts <= 0) throw new ArgumentOutOfRangeException(nameof(parts));
        var each = Math.Round(total / parts, 2, MidpointRounding.ToZero);
        var result = Enumerable.Repeat(each, parts).ToArray();
        result[^1] = total - each * (parts - 1);
        return result;
    }
}

public readonly record struct LineAmounts(decimal Net, decimal Vat, decimal Gross);
