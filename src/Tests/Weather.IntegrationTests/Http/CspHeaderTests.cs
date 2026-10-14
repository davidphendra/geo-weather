using Weather.IntegrationTests.Support;

namespace Weather.IntegrationTests.Http;

/// <summary>The CSP Level 3 header is present on every HTML page (e06s06).</summary>
[Trait("Category", "CspHeaders")]
public sealed class CspHeaderTests(WeatherHostFactory factory) : IClassFixture<WeatherHostFactory>
{
    [Theory]
    [InlineData("/")]
    [InlineData("/favorites")]
    [InlineData("/not-found")]
    public async Task Content_Security_Policy_header_is_present(string path)
    {
        HttpResponseMessage response = await factory.CreateClient().GetAsync(path);

        Assert.True(response.Headers.TryGetValues("Content-Security-Policy", out IEnumerable<string>? values));

        // Blazor adds its own frame-ancestors header; browsers enforce the intersection.
        string policy = string.Join("; ", values ?? []);
        Assert.Contains("frame-ancestors 'none'", policy);
        Assert.Contains("'strict-dynamic'", policy);
        Assert.Contains("default-src 'self'", policy);
    }

    [Fact]
    public async Task Reporting_endpoints_header_is_present()
    {
        HttpResponseMessage response = await factory.CreateClient().GetAsync("/");

        Assert.True(response.Headers.TryGetValues("Reporting-Endpoints", out IEnumerable<string>? values));
        Assert.Equal("csp-endpoint=\"/_csp/report\"", Assert.Single(values ?? []));
    }

    [Fact]
    public async Task Api_responses_are_covered_too()
    {
        HttpResponseMessage response = await factory.CreateClient().GetAsync("/api/countries");

        Assert.True(response.Headers.Contains("Content-Security-Policy"));
    }
}
