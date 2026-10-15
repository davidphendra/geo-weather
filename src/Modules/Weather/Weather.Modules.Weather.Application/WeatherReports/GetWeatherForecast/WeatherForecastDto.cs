using Weather.Modules.Weather.Application.WeatherReports.GetCurrentWeather;

namespace Weather.Modules.Weather.Application.WeatherReports.GetWeatherForecast;

public sealed record WeatherForecastDto(LocationDto Location, IReadOnlyList<ForecastDayDto> Days);

public sealed record ForecastDayDto(
    DateOnly Date,
    TemperatureDto High,
    TemperatureDto Low,
    int PrecipitationProbabilityPercent,
    string SkyCondition);
