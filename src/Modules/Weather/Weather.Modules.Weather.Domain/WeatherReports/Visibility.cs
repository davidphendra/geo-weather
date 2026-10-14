namespace Weather.Modules.Weather.Domain.WeatherReports;

/// <summary>Horizontal visibility in meters.</summary>
public sealed record Visibility
{
    public decimal Meters { get; }

    private Visibility(decimal meters) => Meters = meters;

    public static Visibility FromMeters(decimal meters)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(meters);

        return new Visibility(meters);
    }
}
