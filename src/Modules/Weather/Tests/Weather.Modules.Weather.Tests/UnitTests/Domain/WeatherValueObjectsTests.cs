using global::Weather.Modules.Weather.Domain;
using global::Weather.Modules.Weather.Domain.WeatherReports;

namespace Weather.Modules.Weather.Tests.UnitTests.Domain;

[Trait("Category", "WeatherValueObjects")]
public sealed class WeatherValueObjectsTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(100)]
    public void Humidity_AcceptsRange(int percent)
    {
        Assert.Equal(percent, RelativeHumidity.FromPercent(percent).Percent);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(101)]
    public void Humidity_RejectsOutOfRange(int percent)
    {
        Assert.Throws<WeatherDomainException>(() => RelativeHumidity.FromPercent(percent));
    }

    [Fact]
    public void Visibility_RejectsNegative()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => Visibility.FromMeters(-1m));
    }

    [Fact]
    public void Pressure_StoresHectopascals()
    {
        Assert.Equal(1013m, Pressure.FromHectopascals(1013m).Hectopascals);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    public void Location_RejectsEmptyCity(string? city)
    {
        Assert.ThrowsAny<ArgumentException>(() => Location.Of(city!, "GB"));
    }

    [Theory]
    [InlineData("Clear", "Clear")]
    [InlineData("Clouds", "Cloudy")]
    [InlineData("Rain", "Rain")]
    [InlineData("Mist", "Fog")]
    [InlineData("Tornado", "Unknown")]
    [InlineData(null, "Unknown")]
    public void SkyCondition_MapsProviderValue(string? providerMain, string expected)
    {
        Assert.Equal(expected, SkyCondition.FromProvider(providerMain).Value);
    }
}
