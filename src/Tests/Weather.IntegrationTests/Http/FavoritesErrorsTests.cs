using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

using Weather.IntegrationTests.Support;

namespace Weather.IntegrationTests.Http;

[Trait("Category", "Favorites")]
public sealed class FavoritesErrorsTests(WeatherHostFactory factory) : IClassFixture<WeatherHostFactory>
{
    [Fact]
    public async Task UnknownCity_Returns404()
    {
        HttpResponseMessage response = await factory.CreateClient()
            .PostAsJsonAsync("/api/cities/favorites", new { cityName = "Nowhere", countryCode = "ZZ" });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

        using JsonDocument document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal("RESOURCE_NOT_FOUND", document.RootElement.GetProperty("code").GetString());
        Assert.False(string.IsNullOrWhiteSpace(document.RootElement.GetProperty("traceId").GetString()));
    }

    [Fact]
    public async Task DuplicateFavorite_Returns409()
    {
        HttpClient client = factory.CreateClient();
        await client.PostAsJsonAsync("/api/cities/favorites", new { cityName = "London", countryCode = "GB" });

        HttpResponseMessage response = await client
            .PostAsJsonAsync("/api/cities/favorites", new { cityName = "London", countryCode = "GB" });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);

        using JsonDocument document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal("DUPLICATE_RESOURCE", document.RootElement.GetProperty("code").GetString());
    }

    [Fact]
    public async Task InvalidInput_Returns422()
    {
        HttpResponseMessage response = await factory.CreateClient()
            .PostAsJsonAsync("/api/cities/favorites", new { cityName = string.Empty, countryCode = "GB" });

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);

        using JsonDocument document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal("VALIDATION_FAILED", document.RootElement.GetProperty("code").GetString());
        Assert.NotEmpty(document.RootElement.GetProperty("errors").EnumerateArray());
    }
}
