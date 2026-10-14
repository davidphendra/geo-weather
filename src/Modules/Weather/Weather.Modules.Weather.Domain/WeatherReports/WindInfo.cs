namespace Weather.Modules.Weather.Domain.WeatherReports;

/// <summary>Wind speed (m/s) and an optional bearing in degrees.</summary>
public sealed record WindInfo
{
    public decimal SpeedMetersPerSecond { get; }

    /// <summary>Wind bearing in degrees, or <see langword="null"/> when the provider reports none.</summary>
    public int? DirectionDegrees { get; }

    public string Cardinal => DirectionDegrees is null
        ? "Variable"
        : CardinalDirection.FromDegrees(DirectionDegrees.Value);

    private WindInfo(decimal speedMetersPerSecond, int? directionDegrees)
    {
        SpeedMetersPerSecond = speedMetersPerSecond;
        DirectionDegrees = directionDegrees;
    }

    public static WindInfo FromMetersPerSecond(decimal speedMetersPerSecond, int? directionDegrees)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(speedMetersPerSecond);

        if (directionDegrees is < 0 or > 360)
        {
            throw new WeatherDomainException("Wind direction must be between 0 and 360 degrees.");
        }

        return new WindInfo(speedMetersPerSecond, directionDegrees);
    }
}
