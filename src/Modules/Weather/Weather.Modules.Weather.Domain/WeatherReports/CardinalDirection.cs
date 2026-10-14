namespace Weather.Modules.Weather.Domain.WeatherReports;

/// <summary>Maps a wind bearing in degrees to a 16-point compass direction.</summary>
public static class CardinalDirection
{
    private static readonly string[] Points =
        ["N", "NNE", "NE", "ENE", "E", "ESE", "SE", "SSE", "S", "SSW", "SW", "WSW", "W", "WNW", "NW", "NNW"];

    public static string FromDegrees(int degrees)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(degrees);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(degrees, 360);

        return Points[(int)Math.Round(degrees / 22.5, MidpointRounding.AwayFromZero) % 16];
    }
}
