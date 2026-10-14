using global::Weather.Modules.Weather.Domain.WeatherReports;

namespace Weather.Modules.Weather.Tests.UnitTests.Domain;

[Trait("Category", "Temperature")]
public sealed class TemperatureTests
{
    [Theory]
    [InlineData(32, 0)]
    [InlineData(212, 100)]
    [InlineData(-40, -40)]
    [InlineData(98.6, 37)]
    public void FromFahrenheit_ConvertsToCelsius(decimal fahrenheit, decimal expectedCelsius)
    {
        Temperature temperature = Temperature.FromFahrenheit(fahrenheit);

        Assert.Equal(expectedCelsius, temperature.Celsius);
    }

    [Fact]
    public void FromCelsius_DerivesFahrenheit()
    {
        Assert.Equal(32m, Temperature.FromCelsius(0).Fahrenheit);
        Assert.Equal(212m, Temperature.FromCelsius(100).Fahrenheit);
    }

    [Fact]
    public void RoundsToSingleDecimal()
    {
        // 92°F = 33.333...°C
        Assert.Equal(33.3m, Temperature.FromFahrenheit(92).Celsius);
    }

    [Fact]
    public void Equality_IsByCelsius()
    {
        Assert.Equal(Temperature.FromCelsius(20), Temperature.FromCelsius(20));
        Assert.NotEqual(Temperature.FromCelsius(20), Temperature.FromCelsius(21));
    }
}
