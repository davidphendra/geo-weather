using System.Net;

using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;

using Weather.IntegrationTests.Support;

namespace Weather.IntegrationTests.Http;

/// <summary>One host serves both the Blazor UI and the REST API (ADR-0012).</summary>
[Trait("Category", "SingleHost")]
public sealed class SingleHostTests(WeatherHostFactory factory) : IClassFixture<WeatherHostFactory>
{
    [Fact]
    public async Task ApiRoute_IsServedAsJson()
    {
        HttpResponseMessage response = await factory.CreateClient().GetAsync("/api/countries");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("application/json", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task Home_IsServedAsHtml()
    {
        HttpResponseMessage response = await factory.CreateClient().GetAsync("/");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("text/html", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task UnknownApiRoute_DoesNotReturnTheBlazorNotFoundPage()
    {
        HttpResponseMessage response = await factory.CreateClient().GetAsync("/api/does-not-exist");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.NotEqual("text/html", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task ApiError_CarriesCorrelationIdHeader()
    {
        HttpResponseMessage response = await factory.CreateClient().GetAsync("/api/weather/Nowhere?countryCode=GB");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.True(response.Headers.TryGetValues("X-Correlation-ID", out IEnumerable<string>? values));
        Assert.False(string.IsNullOrWhiteSpace(values?.SingleOrDefault()));
    }

    [Fact]
    public async Task DatabaseFiles_AreNotServedOverHttp()
    {
        string contentRoot = factory.Services.GetRequiredService<IWebHostEnvironment>().ContentRootPath;
        string appDataDirectory = Path.Combine(contentRoot, "App_Data");
        Directory.CreateDirectory(appDataDirectory);

        string contentRootSentinel = Path.Combine(contentRoot, "serving-guard.db");
        string appDataSentinel = Path.Combine(appDataDirectory, "serving-guard.db");
        await File.WriteAllTextAsync(contentRootSentinel, "SQLite format 3");
        await File.WriteAllTextAsync(appDataSentinel, "SQLite format 3");

        try
        {
            HttpResponseMessage appDataResponse = await factory.CreateClient().GetAsync("/App_Data/serving-guard.db");
            HttpResponseMessage contentRootResponse = await factory.CreateClient().GetAsync("/serving-guard.db");

            Assert.Equal(HttpStatusCode.NotFound, appDataResponse.StatusCode);
            Assert.Equal(HttpStatusCode.NotFound, contentRootResponse.StatusCode);
        }
        finally
        {
            File.Delete(contentRootSentinel);
            File.Delete(appDataSentinel);
        }
    }
}
