using Weather.Modules.Favorites.Domain.Countries;

namespace Weather.Modules.Favorites.Domain.Cities;

/// <summary>Local read model of a city, kept current by CityAddedIntegrationEvent.</summary>
public sealed class City
{
    public CityId CityId { get; }

    public CountryCode CountryCode { get; }

    public string Name { get; }

    public GeoCoordinate Coordinate { get; }

    private City(CityId cityId, CountryCode countryCode, string name, GeoCoordinate coordinate)
    {
        CityId = cityId;
        CountryCode = countryCode;
        Name = name;
        Coordinate = coordinate;
    }

    public static City Create(CityId cityId, CountryCode countryCode, string name, GeoCoordinate coordinate)
    {
        ArgumentNullException.ThrowIfNull(cityId);
        ArgumentNullException.ThrowIfNull(countryCode);
        ArgumentNullException.ThrowIfNull(coordinate);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        return new City(cityId, countryCode, name, coordinate);
    }
}
