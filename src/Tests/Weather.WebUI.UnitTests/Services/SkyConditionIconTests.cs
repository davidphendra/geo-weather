using Weather.WebUI.Services;

namespace Weather.WebUI.UnitTests.Services;

/// <summary>The sky-condition to Material Symbols icon mapping (e14s04).</summary>
[Trait("Category", "SkyConditionIcon")]
public sealed class SkyConditionIconTests
{
    [Theory]
    [InlineData("Clear", "sunny")]
    [InlineData("Partly cloudy", "partly_cloudy_day")]
    [InlineData("Cloudy", "cloud")]
    [InlineData("Overcast", "filter_drama")]
    [InlineData("Rain", "rainy")]
    [InlineData("Drizzle", "rainy")]
    [InlineData("Thunderstorm", "thunderstorm")]
    [InlineData("Snow", "weather_snowy")]
    [InlineData("Fog", "foggy")]
    [InlineData("Unknown", "cloud")]
    public void Maps_known_conditions(string condition, string expected) =>
        Assert.Equal(expected, SkyConditionIcon.For(condition));

    [Fact]
    public void Unknown_text_falls_back_to_cloud() =>
        Assert.Equal("cloud", SkyConditionIcon.For("wintry mix"));
}
