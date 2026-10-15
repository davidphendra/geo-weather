using global::Weather.Modules.Weather.Application.Abstractions;
using global::Weather.WebUI.Services;

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Weather.IntegrationTests.Support;

public sealed class WeatherHostFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");

        string geographyDatabase = Path.Combine(Path.GetTempPath(), $"geography-{Guid.NewGuid():N}.db");
        string journalDatabase = Path.Combine(Path.GetTempPath(), $"journal-{Guid.NewGuid():N}.db");

        builder.ConfigureAppConfiguration((_, configuration) => configuration.AddInMemoryCollection(
            new Dictionary<string, string?>
            {
                ["OpenWeather:BaseUrl"] = "https://api.openweathermap.org/",
                ["OpenWeather:ApiKey"] = "test-key",
                ["Logging:LogLevel:System.Net.Http.HttpClient.Weather"] = "Warning",
                ["Csp:EnforceInDevelopment"] = "true",
                ["Observability:Enabled"] = "false",
                ["ConnectionStrings:Geography"] = $"Data Source={geographyDatabase}",
                ["ConnectionStrings:Journal"] = $"Data Source={journalDatabase}",
            }));

        builder.ConfigureServices(services =>
        {
            services.RemoveAll<IWeatherProvider>();
            services.AddSingleton<IWeatherProvider, StubWeatherProvider>();

            // The Blazor pages call the same-origin API through WeatherApiClient; route those calls to
            // this in-memory test server instead of a real socket so page tests exercise real data.
            services.AddHttpClient<WeatherApiClient>()
                .ConfigurePrimaryHttpMessageHandler(() => Server.CreateHandler());
        });
    }
}
