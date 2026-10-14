using global::Weather.Modules.Weather.Application.Abstractions;
using global::Weather.Modules.Weather.Application.WeatherReports.GetCurrentWeather;
using global::Weather.Modules.Weather.Domain.WeatherReports;

using NSubstitute;

namespace Weather.Modules.Weather.Tests.UnitTests.Application;

[Trait("Category", "GetCurrentWeather")]
public sealed class GetCurrentWeatherQueryHandlerTests
{
    private static WeatherReport Report() => WeatherReport.Create(
        Location.Of("London", "GB"),
        DateTimeOffset.UnixEpoch,
        Temperature.FromFahrenheit(50m),
        RelativeHumidity.FromPercent(80),
        WindInfo.FromMetersPerSecond(4m, 220),
        Visibility.FromMeters(10000m),
        Pressure.FromHectopascals(1013m),
        SkyCondition.Cloudy);

    [Fact]
    public async Task ReturnsMappedDto()
    {
        IWeatherProvider service = Substitute.For<IWeatherProvider>();
        service.GetCurrentWeatherAsync("London", "GB", Arg.Any<CancellationToken>()).Returns(Report());
        var handler = new GetCurrentWeatherQueryHandler(service);

        WeatherReportDto? dto = await handler.Handle(
            new GetCurrentWeatherQuery("London", "GB"),
            CancellationToken.None);

        Assert.NotNull(dto);
        Assert.Equal("London", dto!.Location.City);
    }

    [Fact]
    public async Task ReturnsNullWhenServiceFindsNothing()
    {
        IWeatherProvider service = Substitute.For<IWeatherProvider>();
        service.GetCurrentWeatherAsync("Nowhere", "GB", Arg.Any<CancellationToken>())
            .Returns((WeatherReport?)null);
        var handler = new GetCurrentWeatherQueryHandler(service);

        WeatherReportDto? dto = await handler.Handle(
            new GetCurrentWeatherQuery("Nowhere", "GB"),
            CancellationToken.None);

        Assert.Null(dto);
    }
}
