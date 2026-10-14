using FluentValidation;

using global::Weather.Modules.Geography.IntegrationEvents;

using MediatR;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using Weather.BuildingBlocks.Infrastructure;
using Weather.Modules.Notes.Application.Abstractions;
using Weather.Modules.Notes.Application.Cities;
using Weather.Modules.Notes.Application.Configuration;
using Weather.Modules.Notes.Application.Contracts;
using Weather.Modules.Notes.Infrastructure.Database;
using Weather.Modules.Notes.Infrastructure.Repositories;

namespace Weather.Modules.Notes.Infrastructure;

public static class NotesModuleExtensions
{
    public static IServiceCollection AddNotesModule(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.AddDbContext<NotesContext>(options =>
            options.UseSqlite(configuration.GetConnectionString("Notes") ?? "Data Source=notes.db"));

        services.AddScoped<IUnitOfWork>(provider => provider.GetRequiredService<NotesContext>());
        services.AddSingleton<IDateTimeProvider, SystemDateTimeProvider>();
        services.AddScoped<IWeatherNoteRepository, WeatherNoteRepository>();
        services.AddScoped<ICityProjectionRepository, CityProjectionRepository>();

        services.AddMediatR(mediatr => mediatr.RegisterServicesFromAssembly(typeof(INotesModule).Assembly));
        services.AddValidatorsFromAssembly(typeof(INotesModule).Assembly, includeInternalTypes: true);
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
        services.AddScoped<INotesModule, NotesModule>();

        services.AddIntegrationEventHandler<CityAddedIntegrationEvent, CityAddedIntegrationEventHandler>();
        services.AddHostedService<NotesDatabaseInitializer>();

        return services;
    }
}
