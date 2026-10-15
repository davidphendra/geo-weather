using global::Weather.Modules.Weather.Application.Contracts;
using global::Weather.Modules.Weather.Application.WeatherReports.GetCurrentWeather;
using global::Weather.Modules.Weather.Application.WeatherReports.GetWeatherForecast;

using MediatR;

namespace Weather.Modules.Weather.Infrastructure;

internal sealed class WeatherService(ISender sender) : IWeatherService
{
    public Task<WeatherReportDto> GetCurrentWeatherAsync(
        string cityName,
        string? countryCode,
        CancellationToken cancellationToken)
        => sender.Send(new GetCurrentWeatherQuery(cityName, countryCode), cancellationToken);

    public Task<WeatherForecastDto> GetForecastAsync(
        string cityName,
        string? countryCode,
        CancellationToken cancellationToken)
        => sender.Send(new GetWeatherForecastQuery(cityName, countryCode), cancellationToken);
}
