namespace Weather.WebUI.Services;

/// <summary>Client mirror of the Weather current-conditions contract.</summary>
public sealed record WeatherReportModel(
    LocationModel Location,
    decimal? Latitude,
    decimal? Longitude,
    DateTimeOffset ObservedAtUtc,
    TemperatureModel Temperature,
    TemperatureModel DewPoint,
    int RelativeHumidityPercent,
    WindModel Wind,
    decimal VisibilityMeters,
    decimal PressureHectopascals,
    string SkyCondition)
{
    /// <summary>True when both coordinates are present, so a map can be drawn.</summary>
    public bool HasCoordinates => Latitude is not null && Longitude is not null;
}
