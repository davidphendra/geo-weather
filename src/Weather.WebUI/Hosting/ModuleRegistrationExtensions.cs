using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using Weather.BuildingBlocks.Infrastructure;
using Weather.Modules.Favorites.Infrastructure;
using Weather.Modules.Geography.Infrastructure;
using Weather.Modules.Notes.Infrastructure;
using Weather.Modules.Weather.Infrastructure;

namespace Weather.WebUI.Hosting;

/// <summary>Registers the in-memory events bus and the four bounded-context modules.</summary>
internal static class ModuleRegistrationExtensions
{
    public static IServiceCollection AddWeatherModules(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.AddInMemoryEventsBus();
        services.AddWeatherModule(configuration);
        services.AddFavoritesModule(configuration);
        services.AddNotesModule(configuration);
        services.AddGeographyModule(configuration);

        return services;
    }
}
