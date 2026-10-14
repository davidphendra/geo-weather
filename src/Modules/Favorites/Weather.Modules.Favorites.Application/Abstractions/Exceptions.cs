using Weather.Modules.Favorites.Domain.Cities;

namespace Weather.Modules.Favorites.Application.Abstractions;

/// <summary>Thrown when the city is not in the local projection → 404 RESOURCE_NOT_FOUND.</summary>
public sealed class CityNotFoundException(string cityName, string countryCode)
    : Exception($"City '{cityName} ({countryCode})' was not found.");

/// <summary>Thrown when the city is already favorited → 409 DUPLICATE_RESOURCE.</summary>
public sealed class FavoriteAlreadyExistsException(CityId cityId)
    : Exception($"City '{cityId.Value}' is already a favorite.");
