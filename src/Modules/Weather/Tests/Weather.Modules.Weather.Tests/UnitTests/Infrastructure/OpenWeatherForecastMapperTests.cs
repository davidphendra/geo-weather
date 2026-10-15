using global::Weather.Modules.Weather.Application.Abstractions;
using global::Weather.Modules.Weather.Infrastructure.OpenWeatherMap;

namespace Weather.Modules.Weather.Tests.UnitTests.Infrastructure;

[Trait("Category", "OpenWeatherForecastMapping")]
public sealed class OpenWeatherForecastMapperTests
{
    private static long Unix(int year, int month, int day, int hour) =>
        new DateTimeOffset(year, month, day, hour, 0, 0, TimeSpan.Zero).ToUnixTimeSeconds();

    private static OpenWeatherForecastResponse.Step Step(long dt, double tempF, double pop, string main) => new()
    {
        Dt = dt,
        Main = new() { Temp = tempF },
        Pop = pop,
        Weather = [new() { Main = main }],
    };

    private static OpenWeatherForecastResponse Response(
        int timezone,
        params OpenWeatherForecastResponse.Step[] steps) => new()
        {
            City = new() { Name = "London", Country = "GB", Timezone = timezone },
            List = steps,
        };

    [Fact]
    public void MapsThreeHourStepsIntoOneDailyForecast()
    {
        var response = Response(
            timezone: 0,
            Step(Unix(2025, 10, 13, 0), tempF: 50, pop: 0.1, main: "Clouds"),
            Step(Unix(2025, 10, 13, 3), tempF: 59, pop: 0.75, main: "Rain"),
            Step(Unix(2025, 10, 13, 6), tempF: 68, pop: 0.3, main: "Rain"));

        var forecast = OpenWeatherForecastMapper.ToDomain(response);

        Assert.Equal("London", forecast.Location.City);
        Assert.Equal("GB", forecast.Location.CountryCode);

        var day = Assert.Single(forecast.Days);
        Assert.Equal(new DateOnly(2025, 10, 13), day.Date);
        Assert.Equal(20m, day.High.Celsius);   // 68F
        Assert.Equal(10m, day.Low.Celsius);    // 50F
        Assert.Equal(75, day.Precipitation.Percent);
        Assert.Equal("Rain", day.SkyCondition.Value);
    }

    [Fact]
    public void CityOffset_KeepsStepsEitherSideOfUtcMidnightOnTheSameLocalDay()
    {
        var response = Response(
            timezone: 7200, // UTC+02:00
            Step(Unix(2025, 10, 13, 23), tempF: 50, pop: 0.1, main: "Clouds"),
            Step(Unix(2025, 10, 14, 1), tempF: 54, pop: 0.1, main: "Clouds"));

        var forecast = OpenWeatherForecastMapper.ToDomain(response);

        Assert.Equal(new DateOnly(2025, 10, 14), Assert.Single(forecast.Days).Date);
    }

    [Fact]
    public void MalformedProbability_IsWrappedAsProviderException()
    {
        var response = Response(
            timezone: 0,
            Step(Unix(2025, 10, 13, 0), tempF: 50, pop: 1.5, main: "Clouds"));

        Assert.Throws<WeatherProviderException>(() => OpenWeatherForecastMapper.ToDomain(response));
    }
}
