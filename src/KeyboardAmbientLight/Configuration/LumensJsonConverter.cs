using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using KeyboardAmbientLight.Rules;

namespace KeyboardAmbientLight.Configuration;

public sealed class LumensJsonConverter : JsonConverter<double>
{
    public override double Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        return reader.TokenType switch
        {
            JsonTokenType.Number => reader.GetDouble(),
            JsonTokenType.String => ReadString(reader.GetString()),
            _ => throw new JsonException("Lumens must be a number or \"Infinity\".")
        };
    }

    public override void Write(Utf8JsonWriter writer, double value, JsonSerializerOptions options)
    {
        if (double.IsPositiveInfinity(value))
        {
            writer.WriteStringValue("Infinity");
            return;
        }

        writer.WriteNumberValue(value);
    }

    private static double ReadString(string? value)
    {
        if (BrightnessRuleEngine.TryParseLumens(value, out var lumens))
        {
            return lumens;
        }

        throw new JsonException(string.Create(CultureInfo.InvariantCulture, $"Invalid lumens value: {value}"));
    }
}
