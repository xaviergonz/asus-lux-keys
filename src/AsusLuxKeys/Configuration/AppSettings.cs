namespace AsusLuxKeys.Configuration;

public sealed class AppSettings
{
    public bool Enabled { get; set; } = true;

    public string Color { get; set; } = "#FFFFFF";

    public List<BrightnessRule> Rules { get; set; } = [];
}
