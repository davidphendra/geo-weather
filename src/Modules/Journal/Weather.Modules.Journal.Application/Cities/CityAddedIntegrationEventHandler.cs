using global::Weather.Modules.Geography.IntegrationEvents;

using Weather.BuildingBlocks.Application.Events;
using Weather.BuildingBlocks.Domain.SharedKernel;
using Weather.Modules.Journal.Application.Abstractions;
using Weather.Modules.Journal.Domain;
using Weather.Modules.Journal.Domain.Cities;

namespace Weather.Modules.Journal.Application.Cities;

/// <summary>Upserts the local city projection when Geography announces a city.</summary>
internal sealed class CityAddedIntegrationEventHandler(
    ICityProjectionRepository cityProjections,
    IUnitOfWork unitOfWork)
    : IIntegrationEventHandler<CityAddedIntegrationEvent>
{
    public async Task HandleAsync(CityAddedIntegrationEvent integrationEvent, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(integrationEvent);

        City city = City.Create(
            CityId.From(integrationEvent.CityId),
            CountryCode.From(integrationEvent.CountryCode),
            integrationEvent.Name,
            GeoCoordinate.From(integrationEvent.Latitude, integrationEvent.Longitude));

        await cityProjections.UpsertAsync(city, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
