using System.Net;
using System.Text.Json;

using Weather.IntegrationTests.Support;

namespace Weather.IntegrationTests.Http;

[Trait("Category", "ProblemDetails")]
public sealed class ProblemDetailsTests(WeatherHostFactory factory) : IClassFixture<WeatherHostFactory>
{
    [Fact]
    public async Task NotFound_IsProblemDetailsWithCodeAndTraceId()
    {
        HttpResponseMessage response = await factory.CreateClient().GetAsync("/api/weather/Nowhere?countryCode=GB");

        AssertProblemDetails(response, HttpStatusCode.NotFound, "RESOURCE_NOT_FOUND");
    }

    [Fact]
    public async Task ProviderFailure_IsProblemDetailsWithCodeAndTraceId()
    {
        HttpResponseMessage response = await factory.CreateClient().GetAsync("/api/weather/ProviderDown?countryCode=GB");

        AssertProblemDetails(response, HttpStatusCode.BadGateway, "DEPENDENCY_UNAVAILABLE");
    }

    [Fact]
    public async Task UnknownCountry_IsProblemDetailsWithCodeAndTraceId()
    {
        HttpResponseMessage response = await factory.CreateClient().GetAsync("/api/countries/ZZ/cities");

        AssertProblemDetails(response, HttpStatusCode.NotFound, "RESOURCE_NOT_FOUND");
    }

    private static void AssertProblemDetails(HttpResponseMessage response, HttpStatusCode status, string expectedCode)
    {
        Assert.Equal(status, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);

        using JsonDocument document = JsonDocument.Parse(response.Content.ReadAsStringAsync().GetAwaiter().GetResult());
        JsonElement root = document.RootElement;

        Assert.Equal((int)status, root.GetProperty("status").GetInt32());
        Assert.Equal(expectedCode, root.GetProperty("code").GetString());
        Assert.False(string.IsNullOrWhiteSpace(root.GetProperty("traceId").GetString()));
        Assert.True(root.TryGetProperty("title", out _));
        Assert.True(root.TryGetProperty("type", out _));
    }
}
