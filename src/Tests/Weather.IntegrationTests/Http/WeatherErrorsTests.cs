using System.Net;

using Weather.IntegrationTests.Support;

namespace Weather.IntegrationTests.Http;

[Trait("Category", "WeatherErrors")]
public sealed class WeatherErrorsTests(WeatherHostFactory factory) : IClassFixture<WeatherHostFactory>
{
    [Fact]
    public async Task UnknownCity_Returns404()
    {
        HttpResponseMessage response = await factory.CreateClient().GetAsync("/api/weather/Nowhere?countryCode=GB");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task ProviderFailure_Returns502()
    {
        HttpResponseMessage response = await factory.CreateClient().GetAsync("/api/weather/ProviderDown?countryCode=GB");

        Assert.Equal(HttpStatusCode.BadGateway, response.StatusCode);
    }

    [Fact]
    public async Task InvalidInput_Returns422()
    {
        string oversizedCity = new('x', 101);

        HttpResponseMessage response = await factory.CreateClient()
            .GetAsync($"/api/weather/{oversizedCity}?countryCode=GB");

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    }
}
