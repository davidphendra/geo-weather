using MediatR;

using Weather.Modules.Weather.Application.Abstractions;
using Weather.Modules.Weather.Domain.WeatherReports;

namespace Weather.Modules.Weather.Application.WeatherReports.GetCurrentWeather;

internal sealed class GetCurrentWeatherQueryHandler(IWeatherProvider weatherProvider)
    : IRequestHandler<GetCurrentWeatherQuery, WeatherReportDto>
{
    public async Task<WeatherReportDto> Handle(
        GetCurrentWeatherQuery request,
        CancellationToken cancellationToken)
    {
        WeatherReport report = await weatherProvider.GetCurrentWeatherAsync(
            request.CityName,
            request.CountryCode,
            cancellationToken)
            ?? throw new WeatherNotFoundException(request.CityName);

        return report.ToDto();
    }
}
