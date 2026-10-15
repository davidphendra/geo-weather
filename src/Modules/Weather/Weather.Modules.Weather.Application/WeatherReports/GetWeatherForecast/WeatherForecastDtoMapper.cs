using Weather.Modules.Weather.Application.WeatherReports.GetCurrentWeather;
using Weather.Modules.Weather.Domain.Forecasts;

namespace Weather.Modules.Weather.Application.WeatherReports.GetWeatherForecast;

internal static class WeatherForecastDtoMapper
{
    private const int DayCount = 5;

    public static WeatherForecastDto ToDto(this WeatherForecast forecast)
    {
        ArgumentNullException.ThrowIfNull(forecast);

        return new WeatherForecastDto(
            new LocationDto(forecast.Location.City, forecast.Location.CountryCode),
            [
                .. forecast.Days.Take(DayCount).Select(day => new ForecastDayDto(
                    day.Date,
                    new TemperatureDto(day.High.Celsius, day.High.Fahrenheit),
                    new TemperatureDto(day.Low.Celsius, day.Low.Fahrenheit),
                    day.Precipitation.Percent,
                    day.SkyCondition.Value)),
            ]);
    }
}
