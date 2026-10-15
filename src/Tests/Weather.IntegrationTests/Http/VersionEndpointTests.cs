using System.Net;
using System.Text.Json;

using Weather.IntegrationTests.Support;

namespace Weather.IntegrationTests.Http;

/// <summary>The host exposes its build identity at <c>/version</c> for operators and probes (e16s02).</summary>
[Trait("Category", "VersionEndpoint")]
public sealed class VersionEndpointTests(WeatherHostFactory factory) : IClassFixture<WeatherHostFactory>
{
    [Fact]
    public async Task Returns_the_build_version()
    {
        HttpResponseMessage response = await factory.CreateClient().GetAsync("/version");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("application/json", response.Content.Headers.ContentType?.MediaType);

        using JsonDocument document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        JsonElement root = document.RootElement;

        Assert.False(string.IsNullOrWhiteSpace(root.GetProperty("version").GetString()));
        Assert.False(string.IsNullOrWhiteSpace(root.GetProperty("informationalVersion").GetString()));
        Assert.True(root.TryGetProperty("commit", out _));
    }
}
