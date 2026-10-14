using global::Weather.Modules.Geography.IntegrationEvents;

using Weather.BuildingBlocks.Application.Events;
using Weather.Modules.Favorites.Application.Abstractions;
using Weather.Modules.Favorites.Domain;
using Weather.Modules.Favorites.Domain.Cities;
using Weather.Modules.Favorites.Domain.Countries;

namespace Weather.Modules.Favorites.Application.Cities;

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
