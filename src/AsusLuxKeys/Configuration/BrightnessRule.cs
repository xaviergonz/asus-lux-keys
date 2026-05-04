using System.Text.Json.Serialization;
using AsusLuxKeys.Hardware;

namespace AsusLuxKeys.Configuration;

public sealed class BrightnessRule
{
    [JsonConverter(typeof(LumensJsonConverter))]
    public double Lumens { get; set; } = double.PositiveInfinity;

    [JsonConverter(typeof(KeyboardBrightnessJsonConverter))]
    public KeyboardBrightness Brightness { get; set; } = KeyboardBrightness.High;
}
