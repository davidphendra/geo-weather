using global::Weather.Modules.Geography.IntegrationEvents;

using Weather.BuildingBlocks.Application.Events;
using Weather.Modules.Notes.Application.Abstractions;
using Weather.Modules.Notes.Domain.Cities;
using Weather.Modules.Notes.Domain.Countries;

namespace Weather.Modules.Notes.Application.Cities;

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
            integrationEvent.Name);

        await cityProjections.UpsertAsync(city, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
