namespace Weather.Modules.Weather.Domain.WeatherReports;

/// <summary>Current conditions for a location at an instant. Read-only; nothing is persisted.</summary>
public sealed record WeatherReport
{
    public Location Location { get; }

    public Coordinates Coordinates { get; }

    public DateTimeOffset ObservedAtUtc { get; }

    public Temperature Temperature { get; }

    public DewPoint DewPoint { get; }

    public RelativeHumidity RelativeHumidity { get; }

    public WindInfo Wind { get; }

    public Visibility Visibility { get; }

    public Pressure Pressure { get; }

    public SkyCondition SkyCondition { get; }

    private WeatherReport(
        Location location,
        Coordinates coordinates,
        DateTimeOffset observedAtUtc,
        Temperature temperature,
        DewPoint dewPoint,
        RelativeHumidity relativeHumidity,
        WindInfo wind,
        Visibility visibility,
        Pressure pressure,
        SkyCondition skyCondition)
    {
        Location = location;
        Coordinates = coordinates;
        ObservedAtUtc = observedAtUtc;
        Temperature = temperature;
        DewPoint = dewPoint;
        RelativeHumidity = relativeHumidity;
        Wind = wind;
        Visibility = visibility;
        Pressure = pressure;
        SkyCondition = skyCondition;
    }

    public static WeatherReport Create(
        Location location,
        Coordinates coordinates,
        DateTimeOffset observedAtUtc,
        Temperature temperature,
        RelativeHumidity relativeHumidity,
        WindInfo wind,
        Visibility visibility,
        Pressure pressure,
        SkyCondition skyCondition)
    {
        ArgumentNullException.ThrowIfNull(location);
        ArgumentNullException.ThrowIfNull(coordinates);

        if (observedAtUtc.Offset != TimeSpan.Zero)
        {
            throw new WeatherDomainException("Observation time must be UTC.");
        }

        return new WeatherReport(
            location,
            coordinates,
            observedAtUtc,
            temperature,
            DewPoint.Derive(temperature, relativeHumidity),
            relativeHumidity,
            wind,
            visibility,
            pressure,
            skyCondition);
    }
}
