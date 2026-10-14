using global::Weather.Modules.Weather.Application.Abstractions;
using global::Weather.Modules.Weather.Infrastructure;
using global::Weather.Modules.Weather.Infrastructure.OpenWeatherMap;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

using NSubstitute;

namespace Weather.Modules.Weather.Tests.UnitTests.Application;

[Trait("Category", "WeatherServiceWiring")]
public sealed class WeatherModuleWiringTests
{
    [Fact]
    public void RegistersTypedWeatherService()
    {
        using ServiceProvider provider = BuildProvider(Defaults());

        IWeatherProvider service = provider.GetRequiredService<IWeatherProvider>();

        Assert.Equal("OpenWeatherMapService", service.GetType().Name);
    }

    [Fact]
    public void BindsResilienceOptionsFromConfiguration()
    {
        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["OpenWeather:BaseUrl"] = "https://api.openweathermap.org/",
                ["OpenWeather:ApiKey"] = "test-key",
                ["OpenWeather:Resilience:MaxRetryAttempts"] = "5",
                ["OpenWeather:Resilience:RetryBaseDelaySeconds"] = "0.5",
                ["OpenWeather:Resilience:AttemptTimeoutSeconds"] = "8",
                ["OpenWeather:Resilience:TotalTimeoutSeconds"] = "40",
            })
            .Build();

        using ServiceProvider provider = BuildProvider(configuration);

        OpenWeatherResilienceOptions options =
            provider.GetRequiredService<IOptions<OpenWeatherResilienceOptions>>().Value;

        Assert.Equal(5, options.MaxRetryAttempts);
        Assert.Equal(0.5, options.RetryBaseDelaySeconds);
        Assert.Equal(8, options.AttemptTimeoutSeconds);
        Assert.Equal(40, options.TotalTimeoutSeconds);
    }

    [Fact]
    public void UsesDocumentedResilienceDefaults_WhenNotConfigured()
    {
        using ServiceProvider provider = BuildProvider(Defaults());

        OpenWeatherResilienceOptions options =
            provider.GetRequiredService<IOptions<OpenWeatherResilienceOptions>>().Value;

        Assert.Equal(3, options.MaxRetryAttempts);
        Assert.Equal(1, options.RetryBaseDelaySeconds);
        Assert.Equal(10, options.AttemptTimeoutSeconds);
        Assert.Equal(30, options.TotalTimeoutSeconds);
    }

    private static IConfiguration Defaults() => new ConfigurationBuilder()
        .AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["OpenWeather:BaseUrl"] = "https://api.openweathermap.org/",
            ["OpenWeather:ApiKey"] = "test-key",
        })
        .Build();

    private static ServiceProvider BuildProvider(IConfiguration configuration)
    {
        IHostEnvironment environment = Substitute.For<IHostEnvironment>();
        environment.EnvironmentName.Returns("Development");

        var services = new ServiceCollection();
        services.AddSingleton<ILoggerFactory>(NullLoggerFactory.Instance);
        services.AddSingleton(typeof(ILogger<>), typeof(NullLogger<>));
        services.AddSingleton(environment);
        services.AddWeatherModule(configuration);

        return services.BuildServiceProvider();
    }
}
