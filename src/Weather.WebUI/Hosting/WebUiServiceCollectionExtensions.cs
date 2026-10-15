using MediatR;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

using Weather.BuildingBlocks.Application.Telemetry;
using Weather.WebUI.Configuration;
using Weather.WebUI.Security;
using Weather.WebUI.Services;

namespace Weather.WebUI.Hosting;

/// <summary>Registers the Blazor UI, the REST API surface and their shared dependencies.</summary>
internal static class WebUiServiceCollectionExtensions
{
    public static IServiceCollection AddWeatherUi(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNull(environment);

        services.AddWeatherObservability(configuration, environment);

        // Build identity on the console at startup (e16s02).
        services.AddHostedService<StartupVersionLogger>();

        // Telemetry is always emitted into the shared Meter/ActivitySource; the exporter is optional.
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(TelemetryBehavior<,>));
        services.Configure<LoggerFactoryOptions>(options => options.ActivityTrackingOptions =
            ActivityTrackingOptions.TraceId | ActivityTrackingOptions.SpanId);

        services.AddRazorComponents()
            .AddInteractiveServerComponents();

        services.AddControllers();
        services.AddOpenApi();
        services.AddCsp(configuration);
        services.Configure<LocationOptions>(configuration.GetSection(LocationOptions.SectionName));

        // Same-origin REST client for the Blazor UI; the origin is resolved per request from
        // NavigationManager.BaseUri, so there is deliberately no configured base address (D7).
        services.AddHttpClient<WeatherApiClient>();
        services.AddScoped<WeatherExplorerModel>();
        services.AddScoped<CommandDispatchModel>();
        services.AddScoped<EntityFilterModel>();

        return services;
    }
}
