using System.Globalization;
using KeyboardAmbientLight.Configuration;
using KeyboardAmbientLight.Hardware;

namespace KeyboardAmbientLight.Rules;

public static class BrightnessRuleEngine
{
    public static KeyboardBrightness GetBrightness(double lux, IEnumerable<BrightnessRule> rules)
    {
        var parsedRules = ParseRules(rules).ToList();
        foreach (var rule in parsedRules)
        {
            if (lux < rule.Lumens)
            {
                return rule.Brightness;
            }
        }

        return KeyboardBrightness.Off;
    }

    public static List<BrightnessRule> Normalize(IEnumerable<BrightnessRule> rules)
    {
        return ParseRules(rules)
            .Select(rule => new BrightnessRule
            {
                Lumens = rule.Lumens,
                Brightness = rule.Brightness
            })
            .ToList();
    }

    public static string FormatLumens(double lumens)
    {
        return lumens switch
        {
            var infinity when double.IsPositiveInfinity(infinity) => "Infinity",
            var finite => finite.ToString("0.###", CultureInfo.InvariantCulture)
        };
    }

    public static bool TryParseLumens(string? value, out double lumens)
    {
        lumens = 0;
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        var trimmed = value.Trim();
        if (trimmed.Equals("Infinity", StringComparison.OrdinalIgnoreCase) ||
            trimmed.Equals("Infinite", StringComparison.OrdinalIgnoreCase) ||
            trimmed.Equals("Inf", StringComparison.OrdinalIgnoreCase) ||
            trimmed == "\u221e")
        {
            lumens = double.PositiveInfinity;
            return true;
        }

        if (!double.TryParse(trimmed, NumberStyles.Float, CultureInfo.InvariantCulture, out lumens) &&
            !double.TryParse(trimmed, NumberStyles.Float, CultureInfo.CurrentCulture, out lumens))
        {
            return false;
        }

        return double.IsFinite(lumens) && lumens >= 0;
    }

    private static IEnumerable<ParsedRule> ParseRules(IEnumerable<BrightnessRule> rules)
    {
        return rules
            .Select(TryParseRule)
            .OfType<ParsedRule>()
            .OrderBy(rule => rule.Lumens);
    }

    private static ParsedRule? TryParseRule(BrightnessRule rule)
    {
        return IsValidLumens(rule.Lumens)
            ? new ParsedRule(rule.Lumens, KeyboardBrightnessJsonConverter.Normalize(rule.Brightness))
            : null;
    }

    private static bool IsValidLumens(double lumens)
    {
        return (double.IsFinite(lumens) && lumens >= 0) || double.IsPositiveInfinity(lumens);
    }

    private sealed record ParsedRule(double Lumens, KeyboardBrightness Brightness);
}
