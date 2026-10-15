using System.Net;
using System.Text.Json;

using Weather.IntegrationTests.Support;

namespace Weather.IntegrationTests.Http;

/// <summary>Locks each action's declared response outcomes (the Favorites response-contract pattern).</summary>
[Trait("Category", "OpenApiContract")]
public sealed class OpenApiContractTests(WeatherHostFactory factory) : IClassFixture<WeatherHostFactory>
{
    [Theory]
    [InlineData("/api/weather/{cityName}", "get", "200,404,422,500,502")]
    [InlineData("/api/weather/{cityName}/forecast", "get", "200,404,422,500,502")]
    [InlineData("/api/countries", "get", "200,500")]
    [InlineData("/api/countries/{countryCode}/cities", "get", "200,404,422,500")]
    public async Task Action_DeclaresExactlyTheExpectedOutcomes(string path, string method, string expectedCsv)
    {
        string[] expected = expectedCsv.Split(',', StringSplitOptions.RemoveEmptyEntries);

        string[] actual = await GetDeclaredResponseCodesAsync(path, method);

        Assert.Equal(expected.OrderBy(code => code), actual);
    }

    [Fact]
    public async Task NoGetAction_DeclaresMalformedRequest()
    {
        string[] paths =
        [
            "/api/weather/{cityName}",
            "/api/weather/{cityName}/forecast",
            "/api/countries",
            "/api/countries/{countryCode}/cities",
        ];

        foreach (string path in paths)
        {
            Assert.DoesNotContain("400", await GetDeclaredResponseCodesAsync(path, "get"));
        }
    }

    private async Task<string[]> GetDeclaredResponseCodesAsync(string path, string method)
    {
        HttpResponseMessage response = await factory.CreateClient().GetAsync("/openapi/v1.json");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using JsonDocument document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());

        JsonElement responses = document.RootElement
            .GetProperty("paths")
            .GetProperty(path)
            .GetProperty(method)
            .GetProperty("responses");

        return responses
            .EnumerateObject()
            .Select(property => property.Name)
            .OrderBy(name => name)
            .ToArray();
    }
}
