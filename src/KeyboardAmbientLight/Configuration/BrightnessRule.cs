using System.Text.Json.Serialization;
using KeyboardAmbientLight.Hardware;

namespace KeyboardAmbientLight.Configuration;

public sealed class BrightnessRule
{
    [JsonConverter(typeof(LumensJsonConverter))]
    public double Lumens { get; set; } = double.PositiveInfinity;

    [JsonConverter(typeof(KeyboardBrightnessJsonConverter))]
    public KeyboardBrightness Brightness { get; set; } = KeyboardBrightness.High;
}
