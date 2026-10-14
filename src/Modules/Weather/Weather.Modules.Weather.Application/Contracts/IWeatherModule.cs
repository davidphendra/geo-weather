using global::Weather.Modules.Weather.Application.WeatherReports.GetCurrentWeather;

namespace Weather.Modules.Weather.Application.Contracts;

/// <summary>The Weather module's public API. Only the host may call this.</summary>
public interface IWeatherModule
{
    Task<WeatherReportDto?> GetCurrentWeatherAsync(
        string cityName,
        string? countryCode,
        CancellationToken cancellationToken);
}
