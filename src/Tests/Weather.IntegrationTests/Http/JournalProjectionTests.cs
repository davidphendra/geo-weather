using System.Net;
using System.Net.Http.Json;

using Weather.IntegrationTests.Support;

namespace Weather.IntegrationTests.Http;

/// <summary>
/// After the Favorites + Notes merge, one <c>CityAddedIntegrationEvent</c> per seeded city at startup
/// populates the single Journal <c>City</c> projection — so the same city can be both favorited and
/// annotated. (Formerly the multi-subscriber proof; the module is now the sole consumer.)
/// </summary>
[Trait("Category", "JournalProjection")]
public sealed class JournalProjectionTests(WeatherHostFactory factory) : IClassFixture<WeatherHostFactory>
{
    [Fact]
    public async Task OneCatalogEvent_PopulatesTheJournalProjection_EnablingFavoriteAndNote()
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
