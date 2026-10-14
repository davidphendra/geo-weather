using global::Weather.Modules.Weather.Application.Contracts;
using global::Weather.Modules.Weather.Application.WeatherReports.GetCurrentWeather;

using MediatR;

namespace Weather.Modules.Weather.Infrastructure;

internal sealed class WeatherModule(ISender sender) : IWeatherModule
{
    public Task<WeatherReportDto?> GetCurrentWeatherAsync(
        string cityName,
        string? countryCode,
        CancellationToken cancellationToken)
        => sender.Send(new GetCurrentWeatherQuery(cityName, countryCode), cancellationToken);
}
