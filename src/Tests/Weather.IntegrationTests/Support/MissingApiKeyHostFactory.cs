using System.Net;

using global::Weather.Modules.Weather.Application.Abstractions;
using global::Weather.Modules.Weather.Infrastructure.OpenWeatherMap;

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Weather.IntegrationTests.Support;

/// <summary>
/// A Development host with NO <c>OpenWeather:ApiKey</c>. The real provider is kept, but its transport is
/// replaced with an offline handler so the degraded 502 path is exercised without any live network call.
/// </summary>
public sealed class MissingApiKeyHostFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");

        string geography = Path.Combine(Path.GetTempPath(), $"geography-{Guid.NewGuid():N}.db");
        string favorites = Path.Combine(Path.GetTempPath(), $"favorites-{Guid.NewGuid():N}.db");
        string notes = Path.Combine(Path.GetTempPath(), $"notes-{Guid.NewGuid():N}.db");

        builder.ConfigureAppConfiguration((_, configuration) => configuration.AddInMemoryCollection(
            new Dictionary<string, string?>
            {
                ["OpenWeather:BaseUrl"] = "https://api.openweathermap.org/",
                // Deliberately no OpenWeather:ApiKey.
                ["Observability:Enabled"] = "false",
                ["ConnectionStrings:Geography"] = $"Data Source={geography}",
                ["ConnectionStrings:Favorites"] = $"Data Source={favorites}",
                ["ConnectionStrings:Notes"] = $"Data Source={notes}",
            }));

        builder.ConfigureServices(services => services
            .AddHttpClient<IWeatherProvider, OpenWeatherMapService>()
            .ConfigurePrimaryHttpMessageHandler(() => new OfflineHandler()));
    }

    private sealed class OfflineHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.ServiceUnavailable));
    }
}
