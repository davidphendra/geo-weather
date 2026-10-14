using System.Net;
using System.Text.Json;

using Weather.IntegrationTests.Support;

namespace Weather.IntegrationTests.Http;

[Trait("Category", "GeographyEndpoint")]
public sealed class GeographyEndpointTests(WeatherHostFactory factory) : IClassFixture<WeatherHostFactory>
{
    [Fact]
    public async Task ReturnsSeededCountries()
    {
        HttpResponseMessage response = await factory.CreateClient().GetAsync("/api/countries");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using JsonDocument document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        string[] codes = document.RootElement.EnumerateArray()
            .Select(country => country.GetProperty("code").GetString() ?? string.Empty)
            .ToArray();

        Assert.Contains("GB", codes);
        Assert.Contains("US", codes);
    }

    [Fact]
    public async Task Search_FiltersTheCatalog()
    {
        HttpResponseMessage response = await factory.CreateClient().GetAsync("/api/countries?search=united");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using JsonDocument document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        string[] codes = document.RootElement.EnumerateArray()
            .Select(country => country.GetProperty("code").GetString() ?? string.Empty)
            .ToArray();

        Assert.Contains("GB", codes);
        Assert.DoesNotContain("JP", codes);
    }

    [Fact]
    public async Task ReturnsSeededCitiesForCountry()
    {
        HttpResponseMessage response = await factory.CreateClient().GetAsync("/api/countries/GB/cities");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using JsonDocument document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        string[] names = document.RootElement.EnumerateArray()
            .Select(city => city.GetProperty("name").GetString() ?? string.Empty)
            .ToArray();

        Assert.NotEmpty(names);
        Assert.Contains(names, name => name.Contains("London", StringComparison.Ordinal));
    }
}
