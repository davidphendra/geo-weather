namespace Weather.Modules.Weather.Application.Abstractions;

/// <summary>Thrown when weather for a city cannot be resolved → 404 RESOURCE_NOT_FOUND.</summary>
public sealed class WeatherNotFoundException(string cityName)
    : Exception($"Weather for '{cityName}' was not found.");
