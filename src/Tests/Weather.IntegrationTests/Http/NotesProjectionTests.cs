using System.Net;
using System.Net.Http.Json;

using Weather.IntegrationTests.Support;

namespace Weather.IntegrationTests.Http;

/// <summary>
/// Proves the multi-subscriber flow: one <c>CityAddedIntegrationEvent</c> per seeded city at startup populates
/// both the Favorites and the Notes projections — so the same city can be favorited and annotated.
/// </summary>
[Trait("Category", "MultiSubscriber")]
public sealed class NotesProjectionTests(WeatherHostFactory factory) : IClassFixture<WeatherHostFactory>
{
    [Fact]
    public async Task OneCatalogEvent_PopulatesBothFavoritesAndNotesProjections()
    {
        HttpClient client = factory.CreateClient();

        HttpResponseMessage favorite = await client
            .PostAsJsonAsync("/api/cities/favorites", new { cityName = "London", countryCode = "GB" });

        HttpResponseMessage note = await client
            .PostAsJsonAsync(
                "/api/weather/notes",
                new { cityName = "London", countryCode = "GB", note = "Chilly", temperatureCelsius = 12.5 });

        Assert.Equal(HttpStatusCode.Created, favorite.StatusCode);
        Assert.Equal(HttpStatusCode.Created, note.StatusCode);
    }
}
