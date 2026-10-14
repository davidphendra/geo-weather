using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

using Weather.IntegrationTests.Support;

namespace Weather.IntegrationTests.Http;

[Trait("Category", "Favorites")]
public sealed class FavoritesEndpointTests(WeatherHostFactory factory) : IClassFixture<WeatherHostFactory>
{
    [Fact]
    public async Task AddFavorite_ReturnsCreatedWithResourceAndLocation()
    {
        HttpResponseMessage response = await factory.CreateClient()
            .PostAsJsonAsync("/api/cities/favorites", new { cityName = "Paris", countryCode = "FR" });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.NotNull(response.Headers.Location);

        using JsonDocument document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.False(string.IsNullOrWhiteSpace(document.RootElement.GetProperty("id").GetString()));
        Assert.Equal("Paris", document.RootElement.GetProperty("city").GetString());
        Assert.Equal("FR", document.RootElement.GetProperty("countryCode").GetString());
    }

    [Fact]
    public async Task GetFavorites_ReturnsTheSavedList()
    {
        HttpClient client = factory.CreateClient();
        await client.PostAsJsonAsync("/api/cities/favorites", new { cityName = "London", countryCode = "GB" });

        HttpResponseMessage response = await client.GetAsync("/api/cities/favorites");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using JsonDocument document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Contains(
            document.RootElement.EnumerateArray(),
            favorite => favorite.GetProperty("city").GetString() == "London");
    }
}
