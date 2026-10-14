using Microsoft.Extensions.DependencyInjection;

using Weather.BuildingBlocks.Application.Abstractions;

namespace Weather.BuildingBlocks.Infrastructure;

public static class SystemDateTimeProviderRegistration
{
    /// <summary>Registers the system clock as the module's <see cref="IDateTimeProvider"/>.</summary>
    public static IServiceCollection AddSystemDateTimeProvider(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddSingleton<IDateTimeProvider, SystemDateTimeProvider>();

        return services;
    }
}
