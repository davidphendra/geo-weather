using System.Net;
using System.Text;

using global::Weather.Modules.Weather.Application.Abstractions;
using global::Weather.Modules.Weather.Infrastructure.OpenWeatherMap;
using global::Weather.Modules.Weather.Tests.Support;

using Microsoft.Extensions.Logging.Abstractions;

namespace Weather.Modules.Weather.Tests.UnitTests.Infrastructure;

[Trait("Category", "OpenWeatherMapService")]
public sealed class OpenWeatherMapServiceTests
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

    private static OpenWeatherMapService CreateService(HttpMessageHandler innerHandler)
    {
        var httpClient = new HttpClient(innerHandler) { BaseAddress = new Uri("https://api.openweathermap.org/") };

        return new OpenWeatherMapService(httpClient, NullLogger<OpenWeatherMapService>.Instance);
    }

    private static HttpResponseMessage Json(HttpStatusCode status, string payload) => new(status)
    {
        Content = new StringContent(payload, Encoding.UTF8, "application/json"),
    };

    [Fact]
    public async Task Success_ReturnsReport()
    {
        var service = CreateService(new StubHttpMessageHandler(_ => Json(HttpStatusCode.OK, ValidPayload)));

        var report = await service.GetCurrentWeatherAsync("London", "GB", CancellationToken.None);

        Assert.NotNull(report);
        Assert.Equal("London", report!.Location.City);
    }

    [Fact]
    public async Task NotFound_ReturnsNull()
    {
        var service = CreateService(new StubHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.NotFound)));

        var report = await service.GetCurrentWeatherAsync("Nowhere", "GB", CancellationToken.None);

        Assert.Null(report);
    }

    [Fact]
    public async Task ServerError_ThrowsProviderException()
    {
        var service = CreateService(new StubHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.ServiceUnavailable)));

        await Assert.ThrowsAsync<WeatherProviderException>(
            () => service.GetCurrentWeatherAsync("London", "GB", CancellationToken.None));
    }

    [Fact]
    public async Task EmptyBody_ThrowsProviderException()
    {
        var service = CreateService(new StubHttpMessageHandler(_ => Json(HttpStatusCode.OK, "null")));

        await Assert.ThrowsAsync<WeatherProviderException>(
            () => service.GetCurrentWeatherAsync("London", "GB", CancellationToken.None));
    }

    [Fact]
    public async Task TransportFailure_ThrowsProviderException()
    {
        var service = CreateService(new StubHttpMessageHandler(_ => throw new HttpRequestException("no network")));

        await Assert.ThrowsAsync<WeatherProviderException>(
            () => service.GetCurrentWeatherAsync("London", "GB", CancellationToken.None));
    }
}
