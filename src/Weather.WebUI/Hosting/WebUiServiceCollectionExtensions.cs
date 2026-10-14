using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using Weather.WebUI.Security;
using Weather.WebUI.Services;

namespace Weather.WebUI.Hosting;

/// <summary>Registers the Blazor UI, the REST API surface and their shared dependencies.</summary>
internal static class WebUiServiceCollectionExtensions
{
    public static IServiceCollection AddWeatherUi(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.AddRazorComponents()
            .AddInteractiveServerComponents();

        services.AddControllers();
        services.AddOpenApi();
        services.AddCsp(configuration);

        // Same-origin REST client for the Blazor UI; the origin is resolved per request from
        // NavigationManager.BaseUri, so there is deliberately no configured base address (D7).
        services.AddHttpClient<WeatherApiClient>();
        services.AddScoped<WeatherExplorerModel>();
        services.AddScoped<CommandDispatchModel>();
        services.AddScoped<EntityFilterModel>();

        return services;
    }
}
