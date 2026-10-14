using Weather.BuildingBlocks.Application.Events;

namespace Weather.Modules.Geography.IntegrationEvents;

/// <summary>Published by Geography when a city is available in the catalog.</summary>
public sealed record CityAddedIntegrationEvent(
    Guid CityId,
    string CountryCode,
    string Name,
    decimal Latitude,
    decimal Longitude) : IIntegrationEvent;
