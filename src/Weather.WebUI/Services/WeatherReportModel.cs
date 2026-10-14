namespace Weather.WebUI.Services;

/// <summary>Client mirror of the Weather current-conditions contract.</summary>
public sealed record WeatherReportModel(
    LocationModel Location,
    DateTimeOffset ObservedAtUtc,
    TemperatureModel Temperature,
    TemperatureModel DewPoint,
    int RelativeHumidityPercent,
    WindModel Wind,
    decimal VisibilityMeters,
    decimal PressureHectopascals,
    string SkyCondition);

/// <summary>City and country the observation belongs to.</summary>
public sealed record LocationModel(string City, string Country);

/// <summary>A temperature in both units.</summary>
public sealed record TemperatureModel(decimal Celsius, decimal Fahrenheit);

/// <summary>Wind speed, direction and cardinal label.</summary>
public sealed record WindModel(decimal SpeedMetersPerSecond, int? DirectionDegrees, string Cardinal);
