using System.Text.Json;
using System.Text.Json.Serialization;
using KeyboardAmbientLight.Hardware;

namespace KeyboardAmbientLight.Configuration;

public sealed class KeyboardBrightnessJsonConverter : JsonConverter<KeyboardBrightness>
{
    public override KeyboardBrightness Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        return reader.TokenType switch
        {
            JsonTokenType.String => ReadString(reader.GetString()),
            JsonTokenType.Number => FromLegacyPercent(reader.GetInt32()),
            _ => throw new JsonException("Keyboard brightness must be a string enum value or legacy percentage number.")
        };
    }

    public override void Write(Utf8JsonWriter writer, KeyboardBrightness value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(Normalize(value).ToString());
    }

    private static KeyboardBrightness ReadString(string? value)
    {
        if (Enum.TryParse(value, ignoreCase: true, out KeyboardBrightness brightness))
        {
            return Normalize(brightness);
        }

        throw new JsonException($"Invalid keyboard brightness value: {value}");
    }

    private static KeyboardBrightness FromLegacyPercent(int percent)
    {
        return percent switch
        {
            <= 0 => KeyboardBrightness.Off,
            <= 33 => KeyboardBrightness.Low,
            <= 66 => KeyboardBrightness.Medium,
            _ => KeyboardBrightness.High
        };
    }

    public static KeyboardBrightness Normalize(KeyboardBrightness brightness)
    {
        return Enum.IsDefined(brightness) ? brightness : KeyboardBrightness.High;
    }
}
