namespace Weather.Modules.Weather.Application.WeatherReports.GetCurrentWeather;

public sealed record WeatherReportDto(
    LocationDto Location,
    decimal Latitude,
    decimal Longitude,
    DateTimeOffset ObservedAtUtc,
    TemperatureDto Temperature,
    TemperatureDto DewPoint,
    int RelativeHumidityPercent,
    WindDto Wind,
    decimal VisibilityMeters,
    decimal PressureHectopascals,
    string SkyCondition);

public sealed record LocationDto(string City, string Country);

public sealed record TemperatureDto(decimal Celsius, decimal Fahrenheit);

public sealed record WindDto(decimal SpeedMetersPerSecond, int? DirectionDegrees, string Cardinal);
