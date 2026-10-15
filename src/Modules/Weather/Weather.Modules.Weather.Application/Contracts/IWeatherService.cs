using Weather.Modules.Weather.Application.WeatherReports.GetCurrentWeather;
using Weather.Modules.Weather.Application.WeatherReports.GetWeatherForecast;

namespace Weather.Modules.Weather.Application.Contracts;

/// <summary>The Weather module's public API. Only the host may call this.</summary>
public interface IWeatherService
{
    Task<WeatherReportDto> GetCurrentWeatherAsync(
        string cityName,
        string? countryCode,
        CancellationToken cancellationToken);

    Task<WeatherForecastDto> GetForecastAsync(
        string cityName,
        string? countryCode,
        CancellationToken cancellationToken);
}
