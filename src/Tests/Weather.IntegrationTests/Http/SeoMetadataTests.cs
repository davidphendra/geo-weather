using System.Net;

using Weather.IntegrationTests.Support;

namespace Weather.IntegrationTests.Http;

/// <summary>
/// Crawler-facing metadata contract: site-wide tags in the shell, page-specific tags per route,
/// and the discovery files (robots.txt, sitemap.xml).
/// </summary>
[Trait("Category", "SeoMetadata")]
public sealed class SeoMetadataTests(WeatherHostFactory factory) : IClassFixture<WeatherHostFactory>
{
    [Fact]
    public async Task Shell_emits_site_wide_metadata()
    {
        string html = await GetHtmlAsync("/");

        Assert.Contains("name=\"theme-color\" content=\"#131b2e\"", html);
        Assert.Contains("name=\"application-name\" content=\"GeoWeather\"", html);
        Assert.Contains("property=\"og:type\" content=\"website\"", html);
        Assert.Contains("property=\"og:site_name\" content=\"GeoWeather\"", html);
        Assert.Contains("name=\"twitter:card\" content=\"summary_large_image\"", html);
        Assert.Contains("rel=\"apple-touch-icon\" sizes=\"180x180\" href=\"img/apple-touch-icon.png\"", html);
        Assert.Contains("rel=\"manifest\" href=\"/manifest.webmanifest\"", html);
    }

    [Fact]
    public async Task Social_image_is_an_absolute_url()
    {
        string html = await GetHtmlAsync("/");

        Assert.Contains(
            "property=\"og:image\" content=\"http://localhost/img/social-card-1200x630.png\"",
            html);
        Assert.Contains(
            "name=\"twitter:image\" content=\"http://localhost/img/social-card-1200x630.png\"",
            html);
    }

    [Fact]
    public async Task Structured_data_is_emitted_with_the_csp_nonce()
    {
        string html = await GetHtmlAsync("/");

        Assert.Contains("\"@context\": \"https://schema.org\"", html);
        Assert.Contains("\"@type\": \"WebApplication\"", html);
        Assert.Contains("\"url\": \"http://localhost/\"", html);
        // The inline script must carry a nonce, or the CSP would block it in the browser.
        Assert.Contains("json\" nonce=\"", html);
    }

    [Fact]
    public async Task Home_page_owns_its_title_description_and_canonical()
    {
        string html = await GetHtmlAsync("/");

        Assert.Contains("<title>GeoWeather", html);
        Assert.Contains("Weather Explorer</title>", html);
        Assert.Contains(
            "name=\"description\" content=\"Pick a country and city to see current weather conditions at a glance.\"",
            html);
        Assert.Contains("rel=\"canonical\" href=\"http://localhost/\"", html);
        Assert.Contains("name=\"robots\" content=\"index, follow\"", html);
    }

    [Fact]
    public async Task Favorites_page_owns_its_title_and_canonical()
    {
        string html = await GetHtmlAsync("/favorites");

        Assert.Contains("<title>Favorites", html);
        Assert.Contains("Saved Notes", html);
        Assert.Contains("rel=\"canonical\" href=\"http://localhost/favorites\"", html);
    }

    [Theory]
    [InlineData("/not-found")]
    [InlineData("/Error")]
    public async Task Error_routes_are_noindex(string path)
    {
        string html = await GetHtmlAsync(path);

        Assert.Contains("name=\"robots\" content=\"noindex, nofollow\"", html);
        Assert.DoesNotContain("name=\"robots\" content=\"index, follow\"", html);
    }

    [Fact]
    public async Task Robots_txt_points_at_the_sitemap_and_blocks_non_content_routes()
    {
        HttpResponseMessage response = await factory.CreateClient().GetAsync("/robots.txt");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("text/plain", response.Content.Headers.ContentType?.MediaType);

        string body = await response.Content.ReadAsStringAsync();
        Assert.Contains("User-agent: *", body);
        Assert.Contains("Disallow: /api/", body);
        Assert.Contains("Sitemap: http://localhost/sitemap.xml", body);
    }

    [Fact]
    public async Task Sitemap_lists_only_indexable_routes()
    {
        HttpResponseMessage response = await factory.CreateClient().GetAsync("/sitemap.xml");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("application/xml", response.Content.Headers.ContentType?.MediaType);

        string body = await response.Content.ReadAsStringAsync();
        Assert.Contains("<loc>http://localhost/</loc>", body);
        Assert.Contains("<loc>http://localhost/favorites</loc>", body);
        Assert.DoesNotContain("/Error", body);
        Assert.DoesNotContain("/not-found", body);
    }

    [Fact]
    public async Task Web_manifest_is_served_as_a_web_manifest()
    {
        HttpResponseMessage response = await factory.CreateClient().GetAsync("/manifest.webmanifest");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("application/manifest+json", response.Content.Headers.ContentType?.MediaType);
        Assert.Contains("GeoWeather", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Icons_and_social_card_are_served()
    {
        HttpClient client = factory.CreateClient();

        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/img/apple-touch-icon.png")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/img/social-card-1200x630.png")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/img/icon-192.png")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/img/icon-512.png")).StatusCode);
    }

    private async Task<string> GetHtmlAsync(string path)
    {
        HttpResponseMessage response = await factory.CreateClient().GetAsync(path);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("text/html", response.Content.Headers.ContentType?.MediaType);

        return await response.Content.ReadAsStringAsync();
    }
}
