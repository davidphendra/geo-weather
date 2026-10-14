using FluentValidation;

using global::Weather.Modules.Geography.IntegrationEvents;

using MediatR;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using Weather.BuildingBlocks.Infrastructure;
using Weather.Modules.Favorites.Application.Abstractions;
using Weather.Modules.Favorites.Application.Cities;
using Weather.Modules.Favorites.Application.Configuration;
using Weather.Modules.Favorites.Application.Contracts;
using Weather.Modules.Favorites.Infrastructure.Database;
using Weather.Modules.Favorites.Infrastructure.Repositories;

namespace Weather.Modules.Favorites.Infrastructure;

public static class FavoritesModuleExtensions
{
    public static IServiceCollection AddFavoritesModule(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.AddDbContext<FavoritesContext>(options =>
            options.UseSqlite(configuration.GetConnectionString("Favorites") ?? "Data Source=favorites.db"));

        services.AddScoped<IUnitOfWork>(provider => provider.GetRequiredService<FavoritesContext>());
        services.AddSingleton<IDateTimeProvider, SystemDateTimeProvider>();
        services.AddScoped<IFavoriteCityRepository, FavoriteCityRepository>();
        services.AddScoped<IFavoriteCityReadRepository, FavoriteCityReadRepository>();
        services.AddScoped<ICityProjectionRepository, CityProjectionRepository>();

        services.AddMediatR(configuration => configuration.RegisterServicesFromAssembly(typeof(IFavoritesService).Assembly));
        services.AddValidatorsFromAssembly(typeof(IFavoritesService).Assembly, includeInternalTypes: true);
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
        services.AddScoped<IFavoritesService, FavoritesService>();

        services.AddIntegrationEventHandler<CityAddedIntegrationEvent, CityAddedIntegrationEventHandler>();
        services.AddHostedService<FavoritesDatabaseInitializer>();

        return services;
    }
}
