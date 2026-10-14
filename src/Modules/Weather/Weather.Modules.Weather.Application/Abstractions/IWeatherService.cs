using global::Weather.Modules.Weather.Domain.WeatherReports;

namespace Weather.Modules.Weather.Application.Abstractions;

/// <summary>Provider-agnostic port for current-weather retrieval.</summary>
public interface IWeatherService
{
    /// <returns>The current report, or <see langword="null"/> when the city cannot be resolved.</returns>
    Task<WeatherReport?> GetCurrentWeatherAsync(
        string cityName,
        string? countryCode,
        CancellationToken cancellationToken);
}
