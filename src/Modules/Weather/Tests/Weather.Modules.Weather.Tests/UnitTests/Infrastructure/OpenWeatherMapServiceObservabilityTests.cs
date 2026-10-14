using System.Diagnostics.Metrics;
using System.Net;
using System.Text;

using global::Weather.BuildingBlocks.Application.Telemetry;
using global::Weather.Modules.Weather.Application.Abstractions;
using global::Weather.Modules.Weather.Infrastructure.OpenWeatherMap;
using global::Weather.Modules.Weather.Tests.Support;

using Microsoft.Extensions.Logging.Abstractions;

namespace Weather.Modules.Weather.Tests.UnitTests.Infrastructure;

[Trait("Category", "Observability")]
public sealed class OpenWeatherMapServiceObservabilityTests
{
    private const string ValidPayload =
        """
        {
          "name": "London",
          "dt": 1728820800,
          "visibility": 10000,
          "main": { "temp": 50, "pressure": 1013, "humidity": 80 },
          "wind": { "speed": 10, "deg": 220 },
          "sys": { "country": "GB" },
          "weather": [ { "main": "Clouds" } ]
        }
        """;

    [Fact]
    public async Task Success_RecordsAProviderRequest()
    {
        long requests = 0;

        using MeterListener listener = new();
        listener.SetMeasurementEventCallback<long>((instrument, value, _, _) =>
        {
            if (instrument.Name == "weather.provider.requests")
            {
                Interlocked.Add(ref requests, value);
            }
        });
        listener.EnableMeasurementEvents(WeatherMeters.WeatherProviderRequests);
        listener.Start();

        await CreateService(new StubHttpMessageHandler(_ => Json(HttpStatusCode.OK, ValidPayload)))
            .GetCurrentWeatherAsync("London", "GB", CancellationToken.None);

        Assert.Equal(1, Interlocked.Read(ref requests));
    }

    [Fact]
    public async Task NotFound_IsNotCountedAsAFailure()
    {
        long failures = 0;

        using MeterListener listener = new();
        listener.SetMeasurementEventCallback<long>((instrument, value, _, _) =>
        {
            if (instrument.Name == "weather.provider.failures")
            {
                Interlocked.Add(ref failures, value);
            }
        });
        listener.EnableMeasurementEvents(WeatherMeters.WeatherProviderFailures);
        listener.Start();

        await CreateService(new StubHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.NotFound)))
            .GetCurrentWeatherAsync("Nowhere", "GB", CancellationToken.None);

        Assert.Equal(0, Interlocked.Read(ref failures));
    }

    [Fact]
    public async Task ServerError_IsCountedAsABadStatusFailure()
    {
        long failures = 0;

        using MeterListener listener = new();
        listener.SetMeasurementEventCallback<long>((instrument, value, _, _) =>
        {
            if (instrument.Name == "weather.provider.failures")
            {
                Interlocked.Add(ref failures, value);
            }
        });
        listener.EnableMeasurementEvents(WeatherMeters.WeatherProviderFailures);
        listener.Start();

        await Assert.ThrowsAsync<WeatherProviderException>(() =>
            CreateService(new StubHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.ServiceUnavailable)))
                .GetCurrentWeatherAsync("London", "GB", CancellationToken.None));

        Assert.Equal(1, Interlocked.Read(ref failures));
    }

    [Fact]
    public async Task TransportFailure_IsCountedAsUnavailable()
    {
        long failures = 0;

        using MeterListener listener = new();
        listener.SetMeasurementEventCallback<long>((instrument, value, _, _) =>
        {
            if (instrument.Name == "weather.provider.failures")
            {
                Interlocked.Add(ref failures, value);
            }
        });
        listener.EnableMeasurementEvents(WeatherMeters.WeatherProviderFailures);
        listener.Start();

        await Assert.ThrowsAsync<WeatherProviderException>(() =>
            CreateService(new StubHttpMessageHandler(_ => throw new HttpRequestException("no network")))
                .GetCurrentWeatherAsync("London", "GB", CancellationToken.None));

        Assert.Equal(1, Interlocked.Read(ref failures));
    }

    private static OpenWeatherMapService CreateService(HttpMessageHandler handler)
    {
        HttpClient httpClient = new(handler) { BaseAddress = new Uri("https://api.openweathermap.org/") };

        return new OpenWeatherMapService(httpClient, NullLogger<OpenWeatherMapService>.Instance);
    }

    private static HttpResponseMessage Json(HttpStatusCode status, string payload) => new(status)
    {
        Content = new StringContent(payload, Encoding.UTF8, "application/json"),
    };
}
