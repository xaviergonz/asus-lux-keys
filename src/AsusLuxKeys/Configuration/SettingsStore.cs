using System.Drawing;
using System.Globalization;
using System.Text.Json;
using AsusLuxKeys.Logging;
using AsusLuxKeys.Rules;

namespace AsusLuxKeys.Configuration;

public sealed class SettingsStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public string SettingsDirectory { get; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        AppInfo.DisplayName);

    public string SettingsPath => Path.Combine(SettingsDirectory, "settings.json");

    public AppSettings Load()
    {
        try
        {
            if (!File.Exists(SettingsPath))
            {
                var defaults = Normalize(new AppSettings());
                Save(defaults);
                return defaults;
            }

            var json = File.ReadAllText(SettingsPath);
            var settings = JsonSerializer.Deserialize<AppSettings>(json, JsonOptions) ?? new AppSettings();
            return Normalize(settings);
        }
        catch (Exception ex)
        {
            AppLog.Write($"Failed to load settings, using defaults: {ex.Message}");
            return new AppSettings();
        }
    }

    public void Save(AppSettings settings)
    {
        Directory.CreateDirectory(SettingsDirectory);
        var normalized = Normalize(settings);
        var json = JsonSerializer.Serialize(normalized, JsonOptions);
        var tempPath = SettingsPath + ".tmp";

        File.WriteAllText(tempPath, json);
        if (File.Exists(SettingsPath))
        {
            File.Replace(tempPath, SettingsPath, null);
        }
        else
        {
            File.Move(tempPath, SettingsPath);
        }
    }

    public static Color ParseColor(string value)
    {
        try
        {
            return ColorTranslator.FromHtml(value);
        }
        catch
        {
            return Color.White;
        }
    }

    public static string ToHex(Color color)
    {
        return string.Create(CultureInfo.InvariantCulture, $"#{color.R:X2}{color.G:X2}{color.B:X2}");
    }

    private static AppSettings Normalize(AppSettings settings)
    {
        return new AppSettings
        {
            Enabled = settings.Enabled,
            Color = ToHex(ParseColor(settings.Color)),
            Rules = BrightnessRuleEngine.Normalize(settings.Rules ?? [])
        };
    }
}
