namespace Weather.Modules.Notes.Application.Abstractions;

/// <summary>Thrown when the city is not in the local projection → 404 RESOURCE_NOT_FOUND.</summary>
public sealed class CityNotFoundException(string cityName, string countryCode)
    : Exception($"City '{cityName} ({countryCode})' was not found.");
