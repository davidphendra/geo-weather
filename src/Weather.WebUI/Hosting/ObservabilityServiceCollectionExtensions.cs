using Microsoft.OpenTelemetry;

using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

using Weather.BuildingBlocks.Application.Telemetry;
using Weather.WebUI.Configuration;

namespace Weather.WebUI.Hosting;

/// <summary>Wires OpenTelemetry into the host. The host is the only place an exporter is configured.</summary>
internal static class ObservabilityServiceCollectionExtensions
{
    public static IServiceCollection AddWeatherObservability(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNull(environment);

        ObservabilityOptions options = configuration
            .GetSection(ObservabilityOptions.SectionName)
            .Get<ObservabilityOptions>() ?? new ObservabilityOptions();

        if (!options.Enabled)
        {
            // No SDK, no exporter, no socket. Automated tests rely on this.
            return services;
        }

        services.AddOptions<ObservabilityOptions>()
            .Bind(configuration.GetSection(ObservabilityOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddOpenTelemetry()
            .UseMicrosoftOpenTelemetry(distro =>
            {
                distro.Exporters = options.ExportTarget switch
                {
                    ObservabilityExportTarget.Console => ExportTarget.Console,
                    ObservabilityExportTarget.Otlp => ExportTarget.Otlp,
                    _ => ExportTarget.AzureMonitor,
                };

                // TracesPerSecond defaults to 5 and takes precedence over SamplingRatio; null it first.
                distro.AzureMonitor.TracesPerSecond = null;
                distro.AzureMonitor.SamplingRatio = (float)options.SamplingRatio;
            })
            .WithMetrics(metrics => metrics
                .AddMeter(WeatherMeters.MeterName)
                .AddRuntimeInstrumentation()
                .ConfigureResource(resource => resource.AddAttributes(ResourceAttributes(environment))))
            .WithTracing(tracing => tracing
                .AddSource(WeatherActivity.SourceName)
                .ConfigureResource(resource => resource.AddAttributes(ResourceAttributes(environment))));

        return services;
    }

    private static Dictionary<string, object> ResourceAttributes(IHostEnvironment environment) => new()
    {
        ["deployment.environment"] = environment.EnvironmentName,
        ["cloud.provider"] = "azure",
    };
}
