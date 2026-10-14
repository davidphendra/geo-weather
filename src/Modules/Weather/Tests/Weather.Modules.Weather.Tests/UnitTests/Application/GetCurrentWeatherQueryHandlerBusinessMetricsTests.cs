using System.Diagnostics.Metrics;

using global::Weather.BuildingBlocks.Application.Telemetry;
using global::Weather.Modules.Weather.Application.Abstractions;
using global::Weather.Modules.Weather.Application.WeatherReports.GetCurrentWeather;
using global::Weather.Modules.Weather.Domain.WeatherReports;

using NSubstitute;

namespace Weather.Modules.Weather.Tests.UnitTests.Application;

[Trait("Category", "Observability")]
public sealed class GetCurrentWeatherQueryHandlerBusinessMetricsTests
{
    [Fact]
    public async Task Success_RecordsTheSuccessOutcome()
    {
        List<string> outcomes = [];
        using MeterListener listener = ReportListener(outcomes);

        IWeatherProvider provider = Substitute.For<IWeatherProvider>();
        provider.GetCurrentWeatherAsync("London", "GB", Arg.Any<CancellationToken>()).Returns(Report());

        await new GetCurrentWeatherQueryHandler(provider)
            .Handle(new GetCurrentWeatherQuery("London", "GB"), CancellationToken.None);

        Assert.Equal(["success"], outcomes);
    }

    [Fact]
    public async Task UnknownCity_RecordsNotFoundAndTheCityCounter()
    {
        List<string> outcomes = [];
        using MeterListener listener = ReportListener(outcomes);
        long notFound = 0;
        using MeterListener cityListener = CounterListener("weather.city.not_found", v => notFound += v);

        IWeatherProvider provider = Substitute.For<IWeatherProvider>();
        provider.GetCurrentWeatherAsync("Nowhere", "GB", Arg.Any<CancellationToken>())
            .Returns((WeatherReport?)null);

        await Assert.ThrowsAsync<WeatherNotFoundException>(() =>
            new GetCurrentWeatherQueryHandler(provider)
                .Handle(new GetCurrentWeatherQuery("Nowhere", "GB"), CancellationToken.None));

        Assert.Equal(["not_found"], outcomes);
        Assert.Equal(1, notFound);
    }

    [Fact]
    public async Task ProviderFailure_RecordsProviderError()
    {
        List<string> outcomes = [];
        using MeterListener listener = ReportListener(outcomes);

        IWeatherProvider provider = Substitute.For<IWeatherProvider>();
        provider.GetCurrentWeatherAsync("London", "GB", Arg.Any<CancellationToken>())
            .Returns<Task<WeatherReport?>>(_ => throw new WeatherProviderException("down"));

        await Assert.ThrowsAsync<WeatherProviderException>(() =>
            new GetCurrentWeatherQueryHandler(provider)
                .Handle(new GetCurrentWeatherQuery("London", "GB"), CancellationToken.None));

        Assert.Equal(["provider_error"], outcomes);
    }

    private static MeterListener ReportListener(List<string> outcomes)
    {
        MeterListener listener = new();
        listener.SetMeasurementEventCallback<long>((instrument, _, tags, _) =>
        {
            if (instrument.Name != "weather.report.requests")
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
        listener.EnableMeasurementEvents(WeatherMeters.WeatherReportRequests);
        listener.Start();

        return listener;
    }

    private static MeterListener CounterListener(string name, Action<long> onMeasure)
    {
        MeterListener listener = new();
        listener.SetMeasurementEventCallback<long>((instrument, value, _, _) =>
        {
            if (instrument.Name == name)
            {
                onMeasure(value);
            }
        });
        listener.EnableMeasurementEvents(WeatherMeters.WeatherCityNotFound);
        listener.Start();

        return listener;
    }

    private static WeatherReport Report() => WeatherReport.Create(
        Location.Of("London", "GB"),
        DateTimeOffset.UnixEpoch,
        Temperature.FromFahrenheit(50m),
        RelativeHumidity.FromPercent(80),
        WindInfo.FromMetersPerSecond(4m, 220),
        Visibility.FromMeters(10000m),
        Pressure.FromHectopascals(1013m),
        SkyCondition.Cloudy);
}
