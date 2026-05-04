namespace AsusLuxKeys.Hardware;

public static class KeyboardBrightnessExtensions
{
    public static int ToHardwareLevel(this KeyboardBrightness brightness)
    {
        return Math.Clamp((int)brightness, 0, 3);
    }

    public static string ToDisplayText(this KeyboardBrightness brightness)
    {
        return brightness switch
        {
            KeyboardBrightness.Off => "0%",
            KeyboardBrightness.Low => "33%",
            KeyboardBrightness.Medium => "66%",
            KeyboardBrightness.High => "100%",
            _ => "0%"
        };
    }
}
