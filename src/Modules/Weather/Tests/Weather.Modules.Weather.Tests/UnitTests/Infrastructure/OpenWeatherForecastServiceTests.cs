using System.Net;
using System.Text;

using global::Weather.Modules.Weather.Application.Abstractions;
using global::Weather.Modules.Weather.Infrastructure.OpenWeatherMap;
using global::Weather.Modules.Weather.Tests.Support;

using Microsoft.Extensions.Logging.Abstractions;

namespace Weather.Modules.Weather.Tests.UnitTests.Infrastructure;

[Trait("Category", "OpenWeatherForecastService")]
public sealed class OpenWeatherForecastServiceTests
{
    private const string ValidPayload =
        """
        {
          "list": [
            { "dt": 1760313600, "main": { "temp": 50 }, "pop": 0.2, "weather": [ { "main": "Clouds" } ] },
            { "dt": 1760324400, "main": { "temp": 59 }, "pop": 0.8, "weather": [ { "main": "Rain" } ] }
          ],
          "city": { "name": "London", "country": "GB", "timezone": 0 }
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
    public async Task Success_ReturnsForecast()
    {
        var service = CreateService(new StubHttpMessageHandler(_ => Json(HttpStatusCode.OK, ValidPayload)));

        var forecast = await service.GetForecastAsync("London", "GB", CancellationToken.None);

        Assert.NotNull(forecast);
        Assert.Equal("London", forecast!.Location.City);
        Assert.Single(forecast.Days);
    }

    [Fact]
    public async Task UsesTheForecastEndpoint()
    {
        Uri? requested = null;
        var service = CreateService(new StubHttpMessageHandler(request =>
        {
            requested = request.RequestUri;
            return Json(HttpStatusCode.OK, ValidPayload);
        }));

        await service.GetForecastAsync("London", "GB", CancellationToken.None);

        Assert.Equal("/data/2.5/forecast", requested?.AbsolutePath);
    }

    [Fact]
    public async Task NotFound_ReturnsNull()
    {
        var service = CreateService(new StubHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.NotFound)));

        var forecast = await service.GetForecastAsync("Nowhere", "GB", CancellationToken.None);

        Assert.Null(forecast);
    }

    [Fact]
    public async Task ServerError_ThrowsProviderException()
    {
        var service = CreateService(new StubHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.ServiceUnavailable)));

        await Assert.ThrowsAsync<WeatherProviderException>(
            () => service.GetForecastAsync("London", "GB", CancellationToken.None));
    }

    [Fact]
    public async Task EmptyBody_ThrowsProviderException()
    {
        var service = CreateService(new StubHttpMessageHandler(_ => Json(HttpStatusCode.OK, "null")));

        await Assert.ThrowsAsync<WeatherProviderException>(
            () => service.GetForecastAsync("London", "GB", CancellationToken.None));
    }

    [Fact]
    public async Task TransportFailure_ThrowsProviderException()
    {
        var service = CreateService(new StubHttpMessageHandler(_ => throw new HttpRequestException("no network")));

        await Assert.ThrowsAsync<WeatherProviderException>(
            () => service.GetForecastAsync("London", "GB", CancellationToken.None));
    }
}
