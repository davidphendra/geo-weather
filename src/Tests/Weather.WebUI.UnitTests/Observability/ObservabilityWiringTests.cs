using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

using NSubstitute;

using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;

using Weather.WebUI.Hosting;
using Weather.WebUI.Services;

namespace Weather.WebUI.UnitTests.Observability;

[Trait("Category", "Observability")]
public sealed class ObservabilityWiringTests
{
    [Fact]
    public void Disabled_RegistersNoProvider()
    {
        using ServiceProvider provider = Build(enabled: false);

        Assert.Null(provider.GetService<MeterProvider>());
        Assert.Null(provider.GetService<TracerProvider>());
    }

    [Fact]
    public void Enabled_RegistersMeterAndTracerProviders()
    {
        using ServiceProvider provider = Build(enabled: true);

        Assert.NotNull(provider.GetService<MeterProvider>());
        Assert.NotNull(provider.GetService<TracerProvider>());
    }

    [Fact]
    public void Resource_carries_service_identity_and_version()
    {
        IHostEnvironment environment = Substitute.For<IHostEnvironment>();
        environment.EnvironmentName.Returns("Production");

        Dictionary<string, object> attributes =
            ObservabilityServiceCollectionExtensions.ResourceAttributes(environment);

        Assert.Equal("GeoWeather", attributes["service.name"]);
        Assert.Equal(AppVersion.Current.InformationalVersion, attributes["service.version"]);
        Assert.Equal("Production", attributes["deployment.environment"]);
        Assert.Equal("azure", attributes["cloud.provider"]);
    }

    private static ServiceProvider Build(bool enabled)
    {
        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Observability:Enabled"] = enabled ? "true" : "false",
                // Console keeps the unit test offline while still exercising the wiring.
                ["Observability:ExportTarget"] = "Console",
                ["Observability:SamplingRatio"] = "1.0",
            })
            .Build();

        IHostEnvironment environment = Substitute.For<IHostEnvironment>();
        environment.EnvironmentName.Returns("Development");

        ServiceCollection services = new();
        services.AddLogging();
        services.AddWeatherObservability(configuration, environment);

        return services.BuildServiceProvider();
    }
}
