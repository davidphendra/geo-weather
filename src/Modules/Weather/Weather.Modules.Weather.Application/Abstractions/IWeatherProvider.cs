using global::Weather.Modules.Weather.Domain.Forecasts;
using global::Weather.Modules.Weather.Domain.WeatherReports;

namespace Weather.Modules.Weather.Application.Abstractions;

/// <summary>Provider-agnostic port for weather retrieval.</summary>
public interface IWeatherProvider
{
    /// <returns>The current report, or <see langword="null"/> when the city cannot be resolved.</returns>
    Task<WeatherReport?> GetCurrentWeatherAsync(
        string cityName,
        string? countryCode,
        CancellationToken cancellationToken);

    /// <returns>The daily forecast, or <see langword="null"/> when the city cannot be resolved.</returns>
    Task<WeatherForecast?> GetForecastAsync(
        string cityName,
        string? countryCode,
        CancellationToken cancellationToken);
}
