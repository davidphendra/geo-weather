using System.Net;
using System.Text.Json;

using Weather.IntegrationTests.Support;

namespace Weather.IntegrationTests.Http;

[Trait("Category", "WeatherForecast")]
public sealed class WeatherForecastEndpointTests(WeatherHostFactory factory) : IClassFixture<WeatherHostFactory>
{
    [Fact]
    public async Task ReturnsFiveDayPayload()
    {
        HttpResponseMessage response = await factory.CreateClient()
            .GetAsync("/api/weather/London/forecast?countryCode=GB");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using JsonDocument document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        JsonElement root = document.RootElement;

        Assert.Equal("London", root.GetProperty("location").GetProperty("city").GetString());
        Assert.Equal("GB", root.GetProperty("location").GetProperty("country").GetString());

        JsonElement days = root.GetProperty("days");
        Assert.Equal(5, days.GetArrayLength());

        JsonElement day = days[0];
        Assert.True(day.TryGetProperty("date", out _));
        Assert.True(day.GetProperty("high").TryGetProperty("celsius", out _));
        Assert.True(day.GetProperty("high").TryGetProperty("fahrenheit", out _));
        Assert.True(day.GetProperty("low").TryGetProperty("celsius", out _));
        Assert.True(day.TryGetProperty("precipitationProbabilityPercent", out _));
        Assert.True(day.TryGetProperty("skyCondition", out _));
    }

    [Fact]
    public async Task UnknownCity_Returns404()
    {
        HttpResponseMessage response = await factory.CreateClient()
            .GetAsync("/api/weather/Nowhere/forecast?countryCode=GB");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task ProviderFailure_Returns502()
    {
        HttpResponseMessage response = await factory.CreateClient()
            .GetAsync("/api/weather/ProviderDown/forecast?countryCode=GB");

        Assert.Equal(HttpStatusCode.BadGateway, response.StatusCode);
    }

    [Fact]
    public async Task InvalidInput_Returns422()
    {
        string oversizedCity = new('x', 101);

        HttpResponseMessage response = await factory.CreateClient()
            .GetAsync($"/api/weather/{oversizedCity}/forecast?countryCode=GB");

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    }
}
