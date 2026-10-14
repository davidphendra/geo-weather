using global::Weather.Modules.Weather.Domain.WeatherReports;

namespace Weather.Modules.Weather.Tests.UnitTests.Domain;

[Trait("Category", "DewPoint")]
public sealed class DewPointTests
{
    [Fact]
    public void AtFullHumidity_EqualsTemperature()
    {
        Temperature temperature = Temperature.FromCelsius(20);

        DewPoint dewPoint = DewPoint.Derive(temperature, RelativeHumidity.FromPercent(100));

        Assert.Equal(20m, dewPoint.Celsius);
    }

    [Fact]
    public void NeverExceedsTemperature()
    {
        Temperature temperature = Temperature.FromCelsius(20);

        DewPoint dewPoint = DewPoint.Derive(temperature, RelativeHumidity.FromPercent(100));

        Assert.True(dewPoint.Celsius <= temperature.Celsius);
    }

    [Fact]
    public void AtLowerHumidity_IsBelowTemperature()
    {
        Temperature temperature = Temperature.FromCelsius(25);

        DewPoint dewPoint = DewPoint.Derive(temperature, RelativeHumidity.FromPercent(50));

        Assert.True(dewPoint.Celsius < temperature.Celsius);
    }
}
