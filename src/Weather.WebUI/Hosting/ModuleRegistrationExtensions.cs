using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using Weather.BuildingBlocks.Infrastructure;
using Weather.Modules.Geography.Infrastructure;
using Weather.Modules.Journal.Infrastructure;
using Weather.Modules.Weather.Infrastructure;

namespace Weather.WebUI.Hosting;

/// <summary>Registers the in-memory events bus and the three bounded-context modules.</summary>
internal static class ModuleRegistrationExtensions
{
    public static IServiceCollection AddWeatherModules(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.AddInMemoryEventsBus();
        services.AddWeatherModule(configuration);
        services.AddJournalModule(configuration);
        services.AddGeographyModule(configuration);

        return services;
    }
}
