using System.Net;
using System.Net.Http.Json;

using Weather.IntegrationTests.Support;

namespace Weather.IntegrationTests.Http;

/// <summary>
/// Proves the integration-event flow: at startup Geography publishes CityAddedIntegrationEvent for every
/// seeded city, and Favorites upserts its local projection — so a seeded city can then be favorited.
/// </summary>
[Trait("Category", "FavoritesProjection")]
public sealed class FavoritesProjectionTests(WeatherHostFactory factory) : IClassFixture<WeatherHostFactory>
{
    [Fact]
    public async Task CatalogEventPopulatesTheProjection_EnablingAddFavorite()
    {
        HttpResponseMessage response = await factory.CreateClient()
            .PostAsJsonAsync("/api/cities/favorites", new { cityName = "London", countryCode = "GB" });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }
}
