using global::Weather.Modules.Weather.Domain.WeatherReports;

namespace Weather.Modules.Weather.Application.WeatherReports.GetCurrentWeather;

internal static class WeatherReportDtoMapper
{
    public static WeatherReportDto ToDto(this WeatherReport report)
    {
        ArgumentNullException.ThrowIfNull(report);

        return new WeatherReportDto(
            new LocationDto(report.Location.City, report.Location.CountryCode),
            report.Coordinates.Latitude,
            report.Coordinates.Longitude,
            report.ObservedAtUtc,
            new TemperatureDto(report.Temperature.Celsius, report.Temperature.Fahrenheit),
            new TemperatureDto(report.DewPoint.Celsius, report.DewPoint.Fahrenheit),
            report.RelativeHumidity.Percent,
            new WindDto(report.Wind.SpeedMetersPerSecond, report.Wind.DirectionDegrees, report.Wind.Cardinal),
            report.Visibility.Meters,
            report.Pressure.Hectopascals,
            report.SkyCondition.Value);
    }
}
