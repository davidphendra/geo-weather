using System.Net;
using System.Text.Json;

using Weather.IntegrationTests.Support;

namespace Weather.IntegrationTests.Http;

/// <summary>
/// BUG-001 regression: unmatched UI routes render the not-found page, while unmatched API routes
/// stay RFC 9457 Problem Details.
/// </summary>
[Trait("Category", "UiNotFound")]
public sealed class UiNotFoundTests(WeatherHostFactory factory) : IClassFixture<WeatherHostFactory>
{
    [Fact]
    public async Task Unknown_ui_route_renders_the_not_found_page()
    {
        HttpResponseMessage response = await factory.CreateClient().GetAsync("/does-not-exist");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("text/html", response.Content.Headers.ContentType?.MediaType);

        string html = await response.Content.ReadAsStringAsync();
        Assert.Contains("Page not found", html);
    }

    [Fact]
    public async Task Unknown_api_route_stays_problem_details()
    {
        HttpResponseMessage response = await factory.CreateClient().GetAsync("/api/does-not-exist");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);

        using JsonDocument document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal("RESOURCE_NOT_FOUND", document.RootElement.GetProperty("code").GetString());
    }
}
