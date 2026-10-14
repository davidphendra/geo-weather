using global::Weather.Modules.Weather.Application.Abstractions;
using global::Weather.Modules.Weather.Infrastructure;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Weather.Modules.Weather.Tests.UnitTests.Application;

[Trait("Category", "WeatherServiceWiring")]
public sealed class WeatherModuleWiringTests
{
    [Fact]
    public void RegistersTypedWeatherService()
    {
        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["OpenWeather:BaseUrl"] = "https://api.openweathermap.org/",
                ["OpenWeather:ApiKey"] = "test-key",
            })
            .Build();

        var services = new ServiceCollection();
        services.AddSingleton<ILoggerFactory>(NullLoggerFactory.Instance);
        services.AddSingleton(typeof(ILogger<>), typeof(NullLogger<>));
        services.AddWeatherModule(configuration);

        using ServiceProvider provider = services.BuildServiceProvider();

        IWeatherProvider service = provider.GetRequiredService<IWeatherProvider>();

        Assert.Equal("OpenWeatherMapService", service.GetType().Name);
    }
}
