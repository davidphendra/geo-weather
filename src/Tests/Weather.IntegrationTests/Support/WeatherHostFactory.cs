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

        string databasePath = Path.Combine(Path.GetTempPath(), $"geography-{Guid.NewGuid():N}.db");

        builder.ConfigureAppConfiguration((_, configuration) => configuration.AddInMemoryCollection(
            new Dictionary<string, string?>
            {
                ["OpenWeather:BaseUrl"] = "https://api.openweathermap.org/",
                ["OpenWeather:ApiKey"] = "test-key",
                ["Logging:LogLevel:System.Net.Http.HttpClient.Weather"] = "Warning",
                ["ConnectionStrings:Geography"] = $"Data Source={databasePath}",
            }));

        builder.ConfigureServices(services =>
        {
            services.RemoveAll<IWeatherProvider>();
            services.AddSingleton<IWeatherProvider, StubWeatherProvider>();
            services.AddHostedService<GeographyMigrationHostedService>();
        });
    }
}
