using global::Weather.Modules.Weather.Application.Abstractions;

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
        string favoritesDatabase = Path.Combine(Path.GetTempPath(), $"favorites-{Guid.NewGuid():N}.db");
        string notesDatabase = Path.Combine(Path.GetTempPath(), $"notes-{Guid.NewGuid():N}.db");

        builder.ConfigureAppConfiguration((_, configuration) => configuration.AddInMemoryCollection(
            new Dictionary<string, string?>
            {
                ["OpenWeather:BaseUrl"] = "https://api.openweathermap.org/",
                ["OpenWeather:ApiKey"] = "test-key",
                ["Logging:LogLevel:System.Net.Http.HttpClient.Weather"] = "Warning",
                ["Csp:EnforceInDevelopment"] = "true",
                ["ConnectionStrings:Geography"] = $"Data Source={geographyDatabase}",
                ["ConnectionStrings:Favorites"] = $"Data Source={favoritesDatabase}",
                ["ConnectionStrings:Notes"] = $"Data Source={notesDatabase}",
            }));

        builder.ConfigureServices(services =>
        {
            services.RemoveAll<IWeatherProvider>();
            services.AddSingleton<IWeatherProvider, StubWeatherProvider>();
        });
    }
}
