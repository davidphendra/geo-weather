using global::Weather.Modules.Weather.Application.Abstractions;
using global::Weather.Modules.Weather.Application.WeatherReports.GetWeatherForecast;
using global::Weather.Modules.Weather.Domain.Forecasts;
using global::Weather.Modules.Weather.Domain.WeatherReports;

using NSubstitute;

namespace Weather.Modules.Weather.Tests.UnitTests.Application;

[Trait("Category", "GetWeatherForecast")]
public sealed class GetWeatherForecastQueryHandlerTests
{
    private static WeatherForecast Forecast() => WeatherForecast.FromIntervals(
        Location.Of("London", "GB"),
        [
            ForecastInterval.Create(
                DateTimeOffset.UnixEpoch,
                Temperature.FromFahrenheit(50m),
                PrecipitationProbability.FromPercent(20),
                SkyCondition.Cloudy),
        ],
        TimeSpan.Zero);

    [Fact]
    public async Task ReturnsMappedDto()
    {
        IWeatherProvider provider = Substitute.For<IWeatherProvider>();
        provider.GetForecastAsync("London", "GB", Arg.Any<CancellationToken>()).Returns(Forecast());
        var handler = new GetWeatherForecastQueryHandler(provider);

        WeatherForecastDto dto = await handler.Handle(
            new GetWeatherForecastQuery("London", "GB"),
            CancellationToken.None);

        Assert.Equal("London", dto.Location.City);
        Assert.Single(dto.Days);
    }

    [Fact]
    public async Task ThrowsWhenProviderFindsNothing()
    {
        IWeatherProvider provider = Substitute.For<IWeatherProvider>();
        provider.GetForecastAsync("Nowhere", "GB", Arg.Any<CancellationToken>())
            .Returns((WeatherForecast?)null);
        var handler = new GetWeatherForecastQueryHandler(provider);

        await Assert.ThrowsAsync<WeatherNotFoundException>(
            () => handler.Handle(new GetWeatherForecastQuery("Nowhere", "GB"), CancellationToken.None));
    }

    [Fact]
    public async Task PropagatesProviderFailure()
    {
        IWeatherProvider provider = Substitute.For<IWeatherProvider>();
        provider.GetForecastAsync("London", "GB", Arg.Any<CancellationToken>())
            .Returns<Task<WeatherForecast?>>(_ => throw new WeatherProviderException("down"));
        var handler = new GetWeatherForecastQueryHandler(provider);

        await Assert.ThrowsAsync<WeatherProviderException>(
            () => handler.Handle(new GetWeatherForecastQuery("London", "GB"), CancellationToken.None));
    }
}
