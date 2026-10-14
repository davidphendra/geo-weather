using System.Net;

using Weather.IntegrationTests.Support;

namespace Weather.IntegrationTests.Http;

/// <summary>In Development the host must start without an API key and degrade weather calls to 502.</summary>
[Trait("Category", "OpenWeatherConfig")]
public sealed class MissingApiKeyTests(MissingApiKeyHostFactory factory) : IClassFixture<MissingApiKeyHostFactory>
{
    [Fact]
    public async Task Host_StartsWithoutAKey()
    {
        HttpResponseMessage response = await factory.CreateClient().GetAsync("/health/live");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Weather_Returns502WhenTheProviderIsUnavailable()
    {
        HttpResponseMessage response = await factory.CreateClient().GetAsync("/api/weather/London?countryCode=GB");

        Assert.Equal(HttpStatusCode.BadGateway, response.StatusCode);
    }
}
