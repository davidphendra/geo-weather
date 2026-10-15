using global::Weather.Modules.Weather.Application.WeatherReports.GetCurrentWeather;
using global::Weather.Modules.Weather.Domain.WeatherReports;

namespace Weather.Modules.Weather.Tests.UnitTests.Application;

[Trait("Category", "WeatherReportDto")]
public sealed class WeatherReportDtoMapperTests
{
    private static WeatherReport Report(int? directionDegrees) => WeatherReport.Create(
        Location.Of("London", "GB"),
        Coordinates.Of(51.5074m, -0.1278m),
        DateTimeOffset.UnixEpoch,
        Temperature.FromFahrenheit(50m),
        RelativeHumidity.FromPercent(80),
        WindInfo.FromMetersPerSecond(4m, directionDegrees),
        Visibility.FromMeters(10000m),
        Pressure.FromHectopascals(1013m),
        SkyCondition.Cloudy);

    [Fact]
    public void MapsAllFields()
    {
        WeatherReportDto dto = Report(220).ToDto();

        Assert.Equal("London", dto.Location.City);
        Assert.Equal("GB", dto.Location.Country);
        Assert.Equal(10m, dto.Temperature.Celsius);
        Assert.Equal(80, dto.RelativeHumidityPercent);
        Assert.Equal("SW", dto.Wind.Cardinal);
        Assert.Equal(10000m, dto.VisibilityMeters);
        Assert.Equal(1013m, dto.PressureHectopascals);
        Assert.Equal("Cloudy", dto.SkyCondition);
    }

    [Fact]
    public void MissingDirection_MapsAsVariable()
    {
        WeatherReportDto dto = Report(null).ToDto();

        Assert.Null(dto.Wind.DirectionDegrees);
        Assert.Equal("Variable", dto.Wind.Cardinal);
    }
}
