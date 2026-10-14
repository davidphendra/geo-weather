using MediatR;

namespace Weather.Modules.Weather.Application.WeatherReports.GetCurrentWeather;

public sealed record GetCurrentWeatherQuery(string CityName, string? CountryCode) : IRequest<WeatherReportDto>;
