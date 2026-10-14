using System.Net;

using Weather.IntegrationTests.Support;

namespace Weather.IntegrationTests.Http;

[Trait("Category", "OpenApi")]
public sealed class OpenApiTests(WeatherHostFactory factory) : IClassFixture<WeatherHostFactory>
{
    [Fact]
    public async Task ExposesTheOpenApiDocument()
    {
        HttpResponseMessage response = await factory.CreateClient().GetAsync("/openapi/v1.json");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        string document = await response.Content.ReadAsStringAsync();

        Assert.Contains("\"openapi\"", document, StringComparison.Ordinal);
        Assert.Contains("/api/countries", document, StringComparison.Ordinal);
    }
}
