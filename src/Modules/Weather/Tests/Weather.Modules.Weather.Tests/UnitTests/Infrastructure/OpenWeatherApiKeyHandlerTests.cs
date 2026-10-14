using global::Weather.Modules.Weather.Application.Abstractions;
using global::Weather.Modules.Weather.Infrastructure.OpenWeatherMap;
using global::Weather.Modules.Weather.Tests.Support;

using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Weather.Modules.Weather.Tests.UnitTests.Infrastructure;

[Trait("Category", "ApiKeyHandler")]
public sealed class OpenWeatherApiKeyHandlerTests
{
    private const string ApiKey = "SECRET-KEY";

    private static OpenWeatherApiKeyHandler CreateHandler(HttpMessageHandler innerHandler) => new(
        Options.Create(new OpenWeatherOptions { BaseUrl = new Uri("https://api.openweathermap.org/"), ApiKey = ApiKey }))
    {
        InnerHandler = innerHandler,
    };

    [Fact]
    public async Task AppendsApiKeyToRequestUri()
    {
        var captor = new CapturingHttpMessageHandler();
        using var httpClient = new HttpClient(CreateHandler(captor));

        await httpClient.GetAsync("https://api.openweathermap.org/data/2.5/weather?q=London&units=imperial");

        Assert.NotNull(captor.LastRequestUri);
        Assert.Contains($"appid={ApiKey}", captor.LastRequestUri!.Query, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ProviderFailureMessage_DoesNotContainApiKey()
    {
        var throwing = new StubHttpMessageHandler(_ => throw new HttpRequestException("boom"));
        using var httpClient = new HttpClient(CreateHandler(throwing))
        {
            BaseAddress = new Uri("https://api.openweathermap.org/"),
        };
        var service = new OpenWeatherMapService(httpClient, NullLogger<OpenWeatherMapService>.Instance);

        var exception = await Assert.ThrowsAsync<WeatherProviderException>(
            () => service.GetCurrentWeatherAsync("London", "GB", CancellationToken.None));

        Assert.DoesNotContain(ApiKey, exception.Message, StringComparison.Ordinal);
    }
}
