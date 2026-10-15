using global::Weather.Modules.Weather.Application.Abstractions;
using global::Weather.Modules.Weather.Infrastructure.OpenWeatherMap;

namespace Weather.Modules.Weather.Tests.UnitTests.Infrastructure;

[Trait("Category", "OpenWeatherMapping")]
public sealed class OpenWeatherMapMapperTests
{
    private static OpenWeatherMapResponse Response(int? directionDegrees = 220, int humidity = 80) => new()
    {
        Name = "London",
        Dt = 1728820800,
        Visibility = 10000,
        Coord = new() { Lat = 51.5074, Lon = -0.1278 },
        Main = new() { Temp = 50, Pressure = 1013, Humidity = humidity },
        Wind = new() { Speed = 10, Deg = directionDegrees },
        Sys = new() { Country = "GB" },
        Weather = [new() { Main = "Clouds" }],
    };

    [Fact]
    public void MapsImperialPayloadToDomain()
    {
        var report = OpenWeatherMapMapper.ToDomain(Response());

        Assert.Equal("London", report.Location.City);
        Assert.Equal("GB", report.Location.CountryCode);
        Assert.Equal(51.5074m, report.Coordinates.Latitude);
        Assert.Equal(-0.1278m, report.Coordinates.Longitude);
        Assert.Equal(10m, report.Temperature.Celsius);
        Assert.Equal(4.4704m, report.Wind.SpeedMetersPerSecond);
        Assert.Equal(220, report.Wind.DirectionDegrees);
        Assert.Equal("SW", report.Wind.Cardinal);
        Assert.Equal(10000m, report.Visibility.Meters);
        Assert.Equal(1013m, report.Pressure.Hectopascals);
        Assert.Equal("Cloudy", report.SkyCondition.Value);
        Assert.Equal(DateTimeOffset.FromUnixTimeSeconds(1728820800), report.ObservedAtUtc);
    }

    [Fact]
    public void MissingDirection_IsVariable()
    {
        var report = OpenWeatherMapMapper.ToDomain(Response(directionDegrees: null));

        Assert.Null(report.Wind.DirectionDegrees);
        Assert.Equal("Variable", report.Wind.Cardinal);
    }

    [Fact]
    public void MalformedPayload_IsWrappedAsProviderException()
    {
        Assert.Throws<WeatherProviderException>(() => OpenWeatherMapMapper.ToDomain(Response(humidity: 150)));
    }
}
