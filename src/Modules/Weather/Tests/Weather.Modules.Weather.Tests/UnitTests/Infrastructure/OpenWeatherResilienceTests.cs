using System.Globalization;
using System.Net;
using System.Text;

using global::Weather.Modules.Weather.Application.Abstractions;
using global::Weather.Modules.Weather.Tests.Support;

namespace Weather.Modules.Weather.Tests.UnitTests.Infrastructure;

[Trait("Category", "OpenWeatherResilience")]
public sealed class OpenWeatherResilienceTests
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
    public async Task TransientServerErrors_AreRetriedUpToTheConfiguredAttempts()
    {
        var handler = new RecordingHttpMessageHandler(
            () => new HttpResponseMessage(HttpStatusCode.ServiceUnavailable),
            () => new HttpResponseMessage(HttpStatusCode.ServiceUnavailable),
            () => Json(HttpStatusCode.OK, ValidPayload));

        using var harness = new WeatherProviderHarness(handler, Retry(maxRetryAttempts: 2));

        var report = await harness.WeatherProvider.GetCurrentWeatherAsync("London", "GB", CancellationToken.None);

        Assert.NotNull(report);
        Assert.Equal(3, handler.CallCount);
    }

    [Fact]
    public async Task PersistentServerErrors_RetryThenThrow()
    {
        var handler = new RecordingHttpMessageHandler(
            () => new HttpResponseMessage(HttpStatusCode.ServiceUnavailable));

        using var harness = new WeatherProviderHarness(handler, Retry(maxRetryAttempts: 2));

        await Assert.ThrowsAsync<WeatherProviderException>(
            () => harness.WeatherProvider.GetCurrentWeatherAsync("London", "GB", CancellationToken.None));

        Assert.Equal(3, handler.CallCount);
    }

    [Fact]
    public async Task ZeroRetries_MakeASingleAttempt()
    {
        var handler = new RecordingHttpMessageHandler(
            () => new HttpResponseMessage(HttpStatusCode.ServiceUnavailable));

        using var harness = new WeatherProviderHarness(handler, Retry(maxRetryAttempts: 0));

        await Assert.ThrowsAsync<WeatherProviderException>(
            () => harness.WeatherProvider.GetCurrentWeatherAsync("London", "GB", CancellationToken.None));

        Assert.Equal(1, handler.CallCount);
    }

    [Fact]
    public async Task NotFound_IsNotRetried()
    {
        var handler = new RecordingHttpMessageHandler(() => new HttpResponseMessage(HttpStatusCode.NotFound));

        using var harness = new WeatherProviderHarness(handler, Retry(maxRetryAttempts: 3));

        var report = await harness.WeatherProvider.GetCurrentWeatherAsync("Nowhere", "GB", CancellationToken.None);

        Assert.Null(report);
        Assert.Equal(1, handler.CallCount);
    }

    [Fact]
    public async Task TransportFailures_AreRetried()
    {
        var handler = new RecordingHttpMessageHandler(
            () => throw new HttpRequestException("no network"),
            () => throw new HttpRequestException("no network"),
            () => Json(HttpStatusCode.OK, ValidPayload));

        using var harness = new WeatherProviderHarness(handler, Retry(maxRetryAttempts: 2));

        var report = await harness.WeatherProvider.GetCurrentWeatherAsync("London", "GB", CancellationToken.None);

        Assert.NotNull(report);
        Assert.Equal(3, handler.CallCount);
    }

    private static Dictionary<string, string?> Retry(int maxRetryAttempts) => new()
    {
        ["MaxRetryAttempts"] = maxRetryAttempts.ToString(CultureInfo.InvariantCulture),
        ["RetryBaseDelaySeconds"] = "0.01",
        ["AttemptTimeoutSeconds"] = "5",
        ["TotalTimeoutSeconds"] = "20",
    };

    private static HttpResponseMessage Json(HttpStatusCode status, string payload) => new(status)
    {
        Content = new StringContent(payload, Encoding.UTF8, "application/json"),
    };
}
