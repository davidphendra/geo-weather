using global::Weather.Modules.Weather.Application.Abstractions;
using global::Weather.Modules.Weather.Domain.WeatherReports;

using MediatR;

namespace Weather.Modules.Weather.Application.WeatherReports.GetCurrentWeather;

internal sealed class GetCurrentWeatherQueryHandler(IWeatherService weatherService)
    : IRequestHandler<GetCurrentWeatherQuery, WeatherReportDto?>
{
    public async Task<WeatherReportDto?> Handle(
        GetCurrentWeatherQuery request,
        CancellationToken cancellationToken)
    {
        WeatherReport? report = await weatherService.GetCurrentWeatherAsync(
            request.CityName,
            request.CountryCode,
            cancellationToken);

        return report?.ToDto();
    }
}
