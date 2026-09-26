using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace UUSport.Api.Infrastructure;

/// <summary>
/// ADR-002: para, miktar ve oran API'de metin olarak tasinir ("1234.50").
/// Okurken hem metin hem sayi kabul edilir.
/// </summary>
public sealed class DecimalAsStringConverter : JsonConverter<decimal>
{
    public override decimal Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Number) return reader.GetDecimal();
        if (reader.TokenType == JsonTokenType.String)
        {
            var text = reader.GetString();
            if (TextRules.TryParseDecimal(text, out var value)) return value;
            throw new JsonException($"Gecersiz sayi: '{text}'");
        }
        throw new JsonException("Sayi bekleniyordu.");
    }

    public override void Write(Utf8JsonWriter writer, decimal value, JsonSerializerOptions options) =>
        writer.WriteStringValue(value.ToString(CultureInfo.InvariantCulture));
}

public sealed class NullableDecimalAsStringConverter : JsonConverter<decimal?>
{
    private static readonly DecimalAsStringConverter Inner = new();

    public override decimal? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null) return null;
        if (reader.TokenType == JsonTokenType.String && string.IsNullOrWhiteSpace(reader.GetString())) return null;
        return Inner.Read(ref reader, typeof(decimal), options);
    }

    public override void Write(Utf8JsonWriter writer, decimal? value, JsonSerializerOptions options)
    {
        if (value is null) writer.WriteNullValue();
        else Inner.Write(writer, value.Value, options);
    }
}
