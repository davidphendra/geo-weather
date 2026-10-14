using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

using Weather.IntegrationTests.Support;

namespace Weather.IntegrationTests.Http;

[Trait("Category", "Notes")]
public sealed class NotesEndpointTests(WeatherHostFactory factory) : IClassFixture<WeatherHostFactory>
{
    [Fact]
    public async Task AddNote_ReturnsCreatedWithResourceAndLocation()
    {
        HttpResponseMessage response = await factory.CreateClient()
            .PostAsJsonAsync(
                "/api/weather/notes",
                new { cityName = "Paris", countryCode = "FR", note = "Chilly", temperatureCelsius = 12.5 });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.NotNull(response.Headers.Location);

        using JsonDocument document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.False(string.IsNullOrWhiteSpace(document.RootElement.GetProperty("id").GetString()));
        Assert.Equal("Paris", document.RootElement.GetProperty("cityName").GetString());
        Assert.Equal("FR", document.RootElement.GetProperty("countryCode").GetString());
        Assert.Equal("Chilly", document.RootElement.GetProperty("note").GetString());
    }
}
