namespace Weather.Modules.Geography.Domain;

/// <summary>A geographic coordinate.</summary>
public sealed record GeoCoordinate
{
    public decimal Latitude { get; }

    public decimal Longitude { get; }

    private GeoCoordinate(decimal latitude, decimal longitude)
    {
        Latitude = latitude;
        Longitude = longitude;
    }

    public static GeoCoordinate From(decimal latitude, decimal longitude)
    {
        if (latitude is < -90m or > 90m)
        {
            throw new GeographyDomainException("Latitude must be between -90 and 90.");
        }

        if (longitude is < -180m or > 180m)
        {
            throw new GeographyDomainException("Longitude must be between -180 and 180.");
        }

        return new GeoCoordinate(latitude, longitude);
    }
}
