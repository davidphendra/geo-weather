using System.Net;

using Weather.IntegrationTests.Support;

namespace Weather.IntegrationTests.Http;

/// <summary>The Favorites &amp; Saved Notes page renders its workspace (e06s04).</summary>
[Trait("Category", "FavoritesPage")]
public sealed class FavoritesPageTests(WeatherHostFactory factory) : IClassFixture<WeatherHostFactory>
{
    [Fact]
    public async Task Favorites_page_renders_the_workspace()
    {
        HttpResponseMessage response = await factory.CreateClient().GetAsync("/favorites");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        string html = await response.Content.ReadAsStringAsync();
        Assert.Contains("Favorites", html);
        Assert.Contains("CQRS Mutation Feed", html);
        Assert.Contains("Data Pipeline Topology", html);
        Assert.Contains("CreateFavoriteCommand", html);
    }
}
