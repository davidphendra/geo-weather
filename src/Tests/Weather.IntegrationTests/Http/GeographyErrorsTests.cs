using System.Net;

using Weather.IntegrationTests.Support;

namespace Weather.IntegrationTests.Http;

[Trait("Category", "GeographyErrors")]
public sealed class GeographyErrorsTests(WeatherHostFactory factory) : IClassFixture<WeatherHostFactory>
{
    [Fact]
    public async Task UnknownCountry_Returns404()
    {
        HttpResponseMessage response = await factory.CreateClient().GetAsync("/api/countries/ZZ/cities");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
