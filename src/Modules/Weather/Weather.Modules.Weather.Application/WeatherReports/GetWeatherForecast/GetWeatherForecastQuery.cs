using MediatR;

namespace Weather.Modules.Weather.Application.WeatherReports.GetWeatherForecast;

public sealed record GetWeatherForecastQuery(string CityName, string? CountryCode) : IRequest<WeatherForecastDto>;
