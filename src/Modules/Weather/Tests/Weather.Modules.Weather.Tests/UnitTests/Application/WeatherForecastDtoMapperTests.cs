using global::Weather.Modules.Weather.Application.WeatherReports.GetWeatherForecast;
using global::Weather.Modules.Weather.Domain.Forecasts;
using global::Weather.Modules.Weather.Domain.WeatherReports;

namespace Weather.Modules.Weather.Tests.UnitTests.Application;

[Trait("Category", "WeatherForecastDto")]
public sealed class WeatherForecastDtoMapperTests
{
    private static WeatherForecast Forecast(int days) => WeatherForecast.FromIntervals(
        Location.Of("London", "GB"),
        [
            .. Enumerable.Range(0, days).Select(offset => ForecastInterval.Create(
                DateTimeOffset.UnixEpoch.AddDays(offset),
                Temperature.FromFahrenheit(50m + offset),
                PrecipitationProbability.FromPercent(20 + offset),
                SkyCondition.Cloudy)),
        ],
        TimeSpan.Zero);

    [Fact]
    public void MapsAllFields()
    {
        WeatherForecastDto dto = Forecast(1).ToDto();

        Assert.Equal("London", dto.Location.City);
        Assert.Equal("GB", dto.Location.Country);

        ForecastDayDto day = Assert.Single(dto.Days);
        Assert.Equal(new DateOnly(1970, 1, 1), day.Date);
        Assert.Equal(10m, day.High.Celsius);
        Assert.Equal(10m, day.Low.Celsius);
        Assert.Equal(20, day.PrecipitationProbabilityPercent);
        Assert.Equal("Cloudy", day.SkyCondition);
    }

    [Fact]
    public void CapsAtFiveDays()
    {
        WeatherForecastDto dto = Forecast(7).ToDto();

        Assert.Equal(5, dto.Days.Count);
    }
}
