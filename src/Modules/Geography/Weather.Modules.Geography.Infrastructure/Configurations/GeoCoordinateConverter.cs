using System.Globalization;

using Weather.Modules.Geography.Domain;

namespace Weather.Modules.Geography.Infrastructure.Configurations;

/// <summary>Maps <see cref="GeoCoordinate"/> to a single database column.</summary>
internal static class GeoCoordinateConverter
{
    public static string ToProvider(GeoCoordinate coordinate)
    {
        ArgumentNullException.ThrowIfNull(coordinate);

        return string.Create(
            CultureInfo.InvariantCulture,
            $"{coordinate.Latitude}|{coordinate.Longitude}");
    }

    public static GeoCoordinate FromProvider(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);

        string[] parts = value.Split('|');

        return parts.Length != 2
            ? throw new GeographyDomainException($"Invalid stored coordinate '{value}'.")
            : GeoCoordinate.From(
                decimal.Parse(parts[0], CultureInfo.InvariantCulture),
                decimal.Parse(parts[1], CultureInfo.InvariantCulture));
    }
}
