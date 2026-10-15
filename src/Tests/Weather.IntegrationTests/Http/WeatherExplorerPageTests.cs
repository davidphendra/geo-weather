using System.Net;

using Weather.IntegrationTests.Support;

namespace Weather.IntegrationTests.Http;

/// <summary>The Weather Explorer page renders in the shell and survives data/error states (e06s03).</summary>
[Trait("Category", "WeatherExplorerPage")]
public sealed class WeatherExplorerPageTests(WeatherHostFactory factory) : IClassFixture<WeatherHostFactory>
{
    [Fact]
    public async Task Explorer_page_renders_the_query_pipeline()
    {
        HttpResponseMessage response = await factory.CreateClient().GetAsync("/");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        string html = await response.Content.ReadAsStringAsync();
        Assert.Contains("Weather Explorer", html);
        Assert.Contains("CQRS QUERY PIPELINE", html);
        Assert.Contains("5-Day Predictive Ensemble Forecast", html);
        Assert.Contains("href=\"favorites\"", html);
    }
}
