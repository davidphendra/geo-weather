using Weather.Modules.Geography.Domain.Countries;

namespace Weather.Modules.Geography.Domain.Cities;

/// <summary>A city within a country.</summary>
public sealed class City
{
    public CityId Id { get; }

    public CountryCode CountryCode { get; }

    public string Name { get; }

    public GeoCoordinate Coordinate { get; }

    private City(CityId id, CountryCode countryCode, string name, GeoCoordinate coordinate)
    {
        Id = id;
        CountryCode = countryCode;
        Name = name;
        Coordinate = coordinate;
    }

    public static City Create(CityId id, CountryCode countryCode, string name, GeoCoordinate coordinate)
    {
        ArgumentNullException.ThrowIfNull(id);
        ArgumentNullException.ThrowIfNull(countryCode);
        ArgumentNullException.ThrowIfNull(coordinate);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        return new City(id, countryCode, name, coordinate);
    }
}
