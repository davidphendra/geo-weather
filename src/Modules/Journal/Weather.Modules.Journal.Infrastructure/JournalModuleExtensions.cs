using FluentValidation;

using global::Weather.Modules.Geography.IntegrationEvents;

using MediatR;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using Weather.BuildingBlocks.Application.Configuration;
using Weather.BuildingBlocks.Infrastructure;
using Weather.Modules.Journal.Application.Abstractions;
using Weather.Modules.Journal.Application.Cities;
using Weather.Modules.Journal.Application.Contracts;
using Weather.Modules.Journal.Infrastructure.Database;
using Weather.Modules.Journal.Infrastructure.Repositories;

namespace Weather.Modules.Journal.Infrastructure;

public static class JournalModuleExtensions
{
    public static IServiceCollection AddJournalModule(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.AddDbContext<JournalContext>(options =>
            options.UseSqlite(configuration.GetConnectionString("Journal") ?? "Data Source=journal.db"));

        services.AddScoped<IUnitOfWork>(provider => provider.GetRequiredService<JournalContext>());
        services.AddSystemDateTimeProvider();
        services.AddScoped<IFavoriteCityRepository, FavoriteCityRepository>();
        services.AddScoped<IFavoriteCityReadRepository, FavoriteCityReadRepository>();
        services.AddScoped<ICityProjectionRepository, CityProjectionRepository>();
        services.AddScoped<IWeatherNoteRepository, WeatherNoteRepository>();

        services.AddMediatR(configuration => configuration.RegisterServicesFromAssembly(typeof(IFavoritesService).Assembly));
        services.AddValidatorsFromAssembly(typeof(IFavoritesService).Assembly, includeInternalTypes: true);
        services.AddWeatherValidationBehavior();
        services.AddScoped<IFavoritesService, FavoritesService>();
        services.AddScoped<INotesModule, JournalModule>();

        services.AddIntegrationEventHandler<CityAddedIntegrationEvent, CityAddedIntegrationEventHandler>();
        services.AddHostedService<JournalDatabaseInitializer>();

        return services;
    }
}
