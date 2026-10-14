using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

using Weather.BuildingBlocks.Application.Events;
using Weather.Modules.Geography.IntegrationEvents;

namespace Weather.Modules.Geography.Infrastructure.Events;

/// <summary>
/// Publishes a <see cref="CityAddedIntegrationEvent"/> for every city in the catalog when the host starts,
/// so consuming modules can build their own projections.
/// </summary>
internal sealed class CityCatalogEventPublisher(
    IServiceScopeFactory scopeFactory,
    IInMemoryEventsBus eventsBus,
    ILogger<CityCatalogEventPublisher> logger) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        using IServiceScope scope = scopeFactory.CreateScope();
        GeographyContext context = scope.ServiceProvider.GetRequiredService<GeographyContext>();

        List<Domain.Cities.City> cities = await context.Cities
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        logger.LogInformation("Publishing CityAddedIntegrationEvent for {CityCount} cities", cities.Count);

        foreach (Domain.Cities.City city in cities)
        {
            await eventsBus.PublishAsync(
                new CityAddedIntegrationEvent(
                    city.Id.Value,
                    city.CountryCode.Value,
                    city.Name,
                    city.Coordinate.Latitude,
                    city.Coordinate.Longitude),
                cancellationToken);
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
