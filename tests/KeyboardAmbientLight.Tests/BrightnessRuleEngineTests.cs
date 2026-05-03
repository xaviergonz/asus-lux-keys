using KeyboardAmbientLight.Configuration;
using KeyboardAmbientLight.Hardware;
using KeyboardAmbientLight.Rules;
using System.Text.Json;

namespace KeyboardAmbientLight.Tests;

public sealed class BrightnessRuleEngineTests
{
    [Fact]
    public void UsesNextGreaterLumensThreshold()
    {
        var rules = new[]
        {
            new BrightnessRule { Lumens = 10, Brightness = KeyboardBrightness.Low },
            new BrightnessRule { Lumens = 5, Brightness = KeyboardBrightness.High }
        };

        Assert.Equal(KeyboardBrightness.High, BrightnessRuleEngine.GetBrightness(0, rules));
        Assert.Equal(KeyboardBrightness.High, BrightnessRuleEngine.GetBrightness(4.99, rules));
        Assert.Equal(KeyboardBrightness.Low, BrightnessRuleEngine.GetBrightness(5, rules));
        Assert.Equal(KeyboardBrightness.Low, BrightnessRuleEngine.GetBrightness(9.99, rules));
        Assert.Equal(KeyboardBrightness.Off, BrightnessRuleEngine.GetBrightness(10, rules));
    }

    [Fact]
    public void InfinityCatchesValuesAboveTheLastFiniteThreshold()
    {
        var rules = new[]
        {
            new BrightnessRule { Lumens = 10, Brightness = KeyboardBrightness.Medium },
            new BrightnessRule { Lumens = double.PositiveInfinity, Brightness = KeyboardBrightness.Low }
        };

        Assert.Equal(KeyboardBrightness.Medium, BrightnessRuleEngine.GetBrightness(0, rules));
        Assert.Equal(KeyboardBrightness.Medium, BrightnessRuleEngine.GetBrightness(9.99, rules));
        Assert.Equal(KeyboardBrightness.Low, BrightnessRuleEngine.GetBrightness(10, rules));
        Assert.Equal(KeyboardBrightness.Low, BrightnessRuleEngine.GetBrightness(1000, rules));
    }

    [Fact]
    public void FallsBackToOffAboveLastFiniteThreshold()
    {
        var rules = new[]
        {
            new BrightnessRule { Lumens = 1, Brightness = KeyboardBrightness.Low }
        };

        Assert.Equal(KeyboardBrightness.Low, BrightnessRuleEngine.GetBrightness(0.99, rules));
        Assert.Equal(KeyboardBrightness.Off, BrightnessRuleEngine.GetBrightness(1, rules));
        Assert.Equal(KeyboardBrightness.Off, BrightnessRuleEngine.GetBrightness(1000, rules));
    }

    [Fact]
    public void EmptyRulesMeanOff()
    {
        Assert.Equal(KeyboardBrightness.Off, BrightnessRuleEngine.GetBrightness(0, []));
        Assert.Equal(KeyboardBrightness.Off, BrightnessRuleEngine.GetBrightness(1000, []));
    }

    [Theory]
    [InlineData("Infinity", "Infinity")]
    [InlineData("Infinite", "Infinity")]
    [InlineData("5.5000", "5.5")]
    public void NormalizesLumensInput(string input, string expected)
    {
        Assert.True(BrightnessRuleEngine.TryParseLumens(input, out var lumens));
        Assert.Equal(expected, BrightnessRuleEngine.FormatLumens(lumens));
    }

    [Fact]
    public void SerializesFiniteLumensAsJsonNumber()
    {
        var json = JsonSerializer.Serialize(new BrightnessRule { Lumens = 12.5, Brightness = KeyboardBrightness.Medium });

        Assert.Contains("\"Lumens\":12.5", json);
        Assert.Contains("\"Brightness\":\"Medium\"", json);
    }

    [Theory]
    [InlineData("\"Infinity\"")]
    [InlineData("\"infinity\"")]
    [InlineData("\"Inf\"")]
    public void DeserializesInfinityStringsAsPositiveInfinity(string lumensJson)
    {
        var rule = JsonSerializer.Deserialize<BrightnessRule>($$"""{ "Lumens": {{lumensJson}}, "Brightness": 25 }""");

        Assert.NotNull(rule);
        Assert.True(double.IsPositiveInfinity(rule.Lumens));
    }

    [Fact]
    public void DeserializesFiniteLumensJsonNumber()
    {
        var rule = JsonSerializer.Deserialize<BrightnessRule>("""{ "Lumens": 7.25, "Brightness": 25 }""");

        Assert.NotNull(rule);
        Assert.Equal(7.25, rule.Lumens);
        Assert.Equal(KeyboardBrightness.Low, rule.Brightness);
    }

    [Theory]
    [InlineData(0, KeyboardBrightness.Off)]
    [InlineData(33, KeyboardBrightness.Low)]
    [InlineData(50, KeyboardBrightness.Medium)]
    [InlineData(100, KeyboardBrightness.High)]
    public void DeserializesLegacyBrightnessPercentages(int percent, KeyboardBrightness expected)
    {
        var rule = JsonSerializer.Deserialize<BrightnessRule>($$"""{ "Lumens": 7.25, "Brightness": {{percent}} }""");

        Assert.NotNull(rule);
        Assert.Equal(expected, rule.Brightness);
    }
}
