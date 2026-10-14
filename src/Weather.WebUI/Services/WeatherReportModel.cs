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
