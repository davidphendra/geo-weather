namespace Weather.Modules.Weather.Domain.WeatherReports;

/// <summary>A WGS-84 latitude/longitude pair in decimal degrees.</summary>
public sealed record Coordinates
{
    public decimal Latitude { get; }

    public decimal Longitude { get; }

    private Coordinates(decimal latitude, decimal longitude)
    {
        Latitude = latitude;
        Longitude = longitude;
    }

    public static Coordinates Of(decimal latitude, decimal longitude)
    {
        if (latitude is < -90m or > 90m)
        {
            throw new WeatherDomainException("Latitude must be between -90 and 90.");
        }

        if (longitude is < -180m or > 180m)
        {
            throw new WeatherDomainException("Longitude must be between -180 and 180.");
        }

        return new Coordinates(latitude, longitude);
    }
}
