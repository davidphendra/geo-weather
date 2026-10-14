using global::Weather.Modules.Weather.Application.Abstractions;

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

using Weather.IntegrationTests.Support;

namespace Weather.IntegrationTests.Http;

[CollectionDefinition(Name, DisableParallelization = true)]
public sealed class DatabaseConfigurationCollection
{
    public const string Name = "DatabaseConfiguration";
}

/// <summary>The SQLite location is driven by the configurable "Database" section (ADR-0013).</summary>
[Collection(DatabaseConfigurationCollection.Name)]
[Trait("Category", "DatabaseConfiguration")]
public sealed class DatabaseConfigurationTests
{
    [Fact]
    public async Task ConfiguredDataDirectory_ReceivesTheSqliteFiles()
    {
        string dataDirectory = Path.Combine(Path.GetTempPath(), $"weather-data-{Guid.NewGuid():N}");
        Environment.SetEnvironmentVariable("Database__DataDirectory", dataDirectory);

        try
        {
            using DefaultDatabaseHostFactory factory = new();
            await factory.CreateClient().GetAsync("/api/countries");

            Assert.True(File.Exists(Path.Combine(dataDirectory, "geography.db")));
            Assert.True(File.Exists(Path.Combine(dataDirectory, "favorites.db")));
            Assert.True(File.Exists(Path.Combine(dataDirectory, "notes.db")));
        }
        finally
        {
            Environment.SetEnvironmentVariable("Database__DataDirectory", null);
            DeleteDirectory(dataDirectory);
        }
    }

    [Fact]
    public async Task ConnectionString_OverridesConfiguredDataDirectory()
    {
        string dataDirectory = Path.Combine(Path.GetTempPath(), $"weather-data-{Guid.NewGuid():N}");
        string geographyOverride = Path.Combine(Path.GetTempPath(), $"weather-geography-{Guid.NewGuid():N}.db");
        Environment.SetEnvironmentVariable("Database__DataDirectory", dataDirectory);
        Environment.SetEnvironmentVariable("ConnectionStrings__Geography", $"Data Source={geographyOverride}");

        try
        {
            using DefaultDatabaseHostFactory factory = new();
            await factory.CreateClient().GetAsync("/api/countries");

            Assert.True(File.Exists(geographyOverride));
            Assert.False(File.Exists(Path.Combine(dataDirectory, "geography.db")));
            Assert.True(File.Exists(Path.Combine(dataDirectory, "favorites.db")));
        }
        finally
        {
            Environment.SetEnvironmentVariable("Database__DataDirectory", null);
            Environment.SetEnvironmentVariable("ConnectionStrings__Geography", null);
            DeleteDirectory(dataDirectory);
            DeleteFile(geographyOverride);
        }
    }

    private static void DeleteDirectory(string path)
    {
        try
        {
            if (Directory.Exists(path))
            {
                Directory.Delete(path, recursive: true);
            }
        }
        catch (IOException)
        {
            // Best-effort cleanup of a temp directory.
        }
    }

    private static void DeleteFile(string path)
    {
        try
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
        catch (IOException)
        {
            // Best-effort cleanup of a temp file.
        }
    }

    private sealed class DefaultDatabaseHostFactory : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Development");
            builder.ConfigureAppConfiguration((_, configuration) => configuration.AddInMemoryCollection(
                new Dictionary<string, string?>
                {
                    ["OpenWeather:BaseUrl"] = "https://api.openweathermap.org/",
                    ["OpenWeather:ApiKey"] = "test-key",
                    ["Logging:LogLevel:System.Net.Http.HttpClient.Weather"] = "Warning",
                }));
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<IWeatherProvider>();
                services.AddSingleton<IWeatherProvider, StubWeatherProvider>();
            });
        }
    }
}
