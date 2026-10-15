using System.Net;
using System.Text.Json;

using Weather.IntegrationTests.Support;

namespace Weather.IntegrationTests.Http;

[Trait("Category", "WeatherEndpoint")]
public sealed class WeatherEndpointTests(WeatherHostFactory factory) : IClassFixture<WeatherHostFactory>
{
    [Fact]
    public async Task ReturnsCompletePayload()
    {
        HttpClient client = factory.CreateClient();

        HttpResponseMessage response = await client.GetAsync("/api/weather/London?countryCode=GB");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using JsonDocument document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        JsonElement root = document.RootElement;

        Assert.Equal("London", root.GetProperty("location").GetProperty("city").GetString());
        Assert.Equal("GB", root.GetProperty("location").GetProperty("country").GetString());
        Assert.Equal(51.5074m, root.GetProperty("latitude").GetDecimal());
        Assert.Equal(-0.1278m, root.GetProperty("longitude").GetDecimal());
        Assert.True(root.TryGetProperty("observedAtUtc", out _));
        Assert.True(root.GetProperty("temperature").TryGetProperty("celsius", out _));
        Assert.True(root.GetProperty("temperature").TryGetProperty("fahrenheit", out _));
        Assert.True(root.GetProperty("dewPoint").TryGetProperty("celsius", out _));
        Assert.True(root.GetProperty("dewPoint").TryGetProperty("fahrenheit", out _));
        Assert.True(root.TryGetProperty("relativeHumidityPercent", out _));
        Assert.True(root.GetProperty("wind").TryGetProperty("speedMetersPerSecond", out _));
        Assert.True(root.GetProperty("wind").TryGetProperty("directionDegrees", out _));
        Assert.True(root.TryGetProperty("visibilityMeters", out _));
        Assert.True(root.TryGetProperty("pressureHectopascals", out _));
        Assert.True(root.TryGetProperty("skyCondition", out _));
    }
}
