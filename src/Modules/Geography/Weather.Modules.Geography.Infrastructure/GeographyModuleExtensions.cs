using FluentValidation;

using MediatR;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using Weather.Modules.Geography.Application.Abstractions;
using Weather.Modules.Geography.Application.Configuration;
using Weather.Modules.Geography.Application.Contracts;
using Weather.Modules.Geography.Infrastructure.Database;
using Weather.Modules.Geography.Infrastructure.Events;
using Weather.Modules.Geography.Infrastructure.Repositories;

namespace Weather.Modules.Geography.Infrastructure;

public static class GeographyModuleExtensions
{
    public static IServiceCollection AddGeographyModule(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.AddDbContext<GeographyContext>(options =>
            options.UseSqlite(configuration.GetConnectionString("Geography") ?? "Data Source=geography.db"));

        services.AddScoped<ICountryReadRepository, CountryReadRepository>();
        services.AddScoped<ICityReadRepository, CityReadRepository>();
        services.AddMediatR(configuration => configuration.RegisterServicesFromAssembly(typeof(IGeographyService).Assembly));
        services.AddValidatorsFromAssembly(typeof(IGeographyService).Assembly, includeInternalTypes: true);
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
        services.AddScoped<IGeographyService, GeographyService>();

        services.AddHostedService<GeographyDatabaseInitializer>();
        services.AddHostedService<CityCatalogEventPublisher>();

        return services;
    }
}
