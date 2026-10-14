using global::Weather.Modules.Weather.Application.Abstractions;
using global::Weather.Modules.Weather.Infrastructure;
using global::Weather.Modules.Weather.Infrastructure.OpenWeatherMap;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

using NSubstitute;

namespace Weather.Modules.Weather.Tests.Support;

/// <summary>
/// Boots the Weather module's real composition root (<see cref="WeatherModuleExtensions.AddWeatherModule"/>)
/// over a stub primary handler, so tests exercise the actual typed client, key handler, and resilience
/// pipeline without any network access.
/// </summary>
internal sealed class WeatherProviderHarness : IDisposable
{
    private readonly ServiceProvider _provider;

    public WeatherProviderHarness(
        HttpMessageHandler primaryHandler,
        IReadOnlyDictionary<string, string?>? resilience = null)
    {
        Dictionary<string, string?> values = new()
        {
            ["OpenWeather:BaseUrl"] = "https://api.openweathermap.org/",
            ["OpenWeather:ApiKey"] = "test-key",
        };

        if (resilience is not null)
        {
            foreach ((string key, string? value) in resilience)
            {
                values[$"OpenWeather:Resilience:{key}"] = value;
            }
        }

        IConfiguration configuration = new ConfigurationBuilder().AddInMemoryCollection(values).Build();

        IHostEnvironment environment = Substitute.For<IHostEnvironment>();
        environment.EnvironmentName.Returns("Development");

        var services = new ServiceCollection();
        services.AddSingleton<ILoggerFactory>(NullLoggerFactory.Instance);
        services.AddSingleton(typeof(ILogger<>), typeof(NullLogger<>));
        services.AddSingleton(environment);
        services.AddWeatherModule(configuration);

        services.AddHttpClient<IWeatherProvider, OpenWeatherMapService>()
            .ConfigurePrimaryHttpMessageHandler(() => primaryHandler);

        _provider = services.BuildServiceProvider();
        WeatherProvider = _provider.GetRequiredService<IWeatherProvider>();
    }

    public IWeatherProvider WeatherProvider { get; }

    public void Dispose() => _provider.Dispose();
}
