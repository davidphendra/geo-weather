using System.Diagnostics.Metrics;

using global::Weather.BuildingBlocks.Application.Telemetry;
using global::Weather.Modules.Weather.Application.Abstractions;
using global::Weather.Modules.Weather.Application.WeatherReports.GetWeatherForecast;
using global::Weather.Modules.Weather.Domain.Forecasts;
using global::Weather.Modules.Weather.Domain.WeatherReports;

using NSubstitute;

namespace Weather.Modules.Weather.Tests.UnitTests.Application;

[Trait("Category", "ForecastObservability")]
public sealed class GetWeatherForecastQueryHandlerBusinessMetricsTests
{
    [Fact]
    public async Task Success_RecordsTheSuccessOutcome()
    {
        List<string> outcomes = [];
        using MeterListener listener = OutcomeListener(outcomes);

        IWeatherProvider provider = Substitute.For<IWeatherProvider>();
        provider.GetForecastAsync("London", "GB", Arg.Any<CancellationToken>()).Returns(Forecast());

        await new GetWeatherForecastQueryHandler(provider)
            .Handle(new GetWeatherForecastQuery("London", "GB"), CancellationToken.None);

        Assert.Equal(["success"], outcomes);
    }

    [Fact]
    public async Task UnknownCity_RecordsNotFound()
    {
        List<string> outcomes = [];
        using MeterListener listener = OutcomeListener(outcomes);

        IWeatherProvider provider = Substitute.For<IWeatherProvider>();
        provider.GetForecastAsync("Nowhere", "GB", Arg.Any<CancellationToken>())
            .Returns((WeatherForecast?)null);

        await Assert.ThrowsAsync<WeatherNotFoundException>(() =>
            new GetWeatherForecastQueryHandler(provider)
                .Handle(new GetWeatherForecastQuery("Nowhere", "GB"), CancellationToken.None));

        Assert.Equal(["not_found"], outcomes);
    }

    [Fact]
    public async Task ProviderFailure_RecordsProviderError()
    {
        List<string> outcomes = [];
        using MeterListener listener = OutcomeListener(outcomes);

        IWeatherProvider provider = Substitute.For<IWeatherProvider>();
        provider.GetForecastAsync("London", "GB", Arg.Any<CancellationToken>())
            .Returns<Task<WeatherForecast?>>(_ => throw new WeatherProviderException("down"));

        await Assert.ThrowsAsync<WeatherProviderException>(() =>
            new GetWeatherForecastQueryHandler(provider)
                .Handle(new GetWeatherForecastQuery("London", "GB"), CancellationToken.None));

        Assert.Equal(["provider_error"], outcomes);
    }

    private static MeterListener OutcomeListener(List<string> outcomes)
    {
        MeterListener listener = new();
        listener.SetMeasurementEventCallback<long>((instrument, _, tags, _) =>
        {
            if (instrument.Name != "weather.forecast.requests")
            {
                return;
            }

            foreach (KeyValuePair<string, object?> tag in tags)
            {
                if (tag.Key == WeatherMeters.Tags.Outcome)
                {
                    outcomes.Add(tag.Value as string ?? string.Empty);
                }
            }
        });
        listener.EnableMeasurementEvents(WeatherMeters.WeatherForecastRequests);
        listener.Start();

        return listener;
    }

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
}
