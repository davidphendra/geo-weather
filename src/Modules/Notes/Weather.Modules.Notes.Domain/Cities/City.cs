using Weather.BuildingBlocks.Domain.SharedKernel;

namespace Weather.Modules.Notes.Domain.Cities;

/// <summary>
/// Local read model of a city, kept current by <c>CityAddedIntegrationEvent</c>. Minimal on purpose: Notes only
/// needs to resolve a city by name and country to validate a note.
/// </summary>
public sealed class City
{
    public CityId CityId { get; }

    public CountryCode CountryCode { get; }

    public string Name { get; }

    private City(CityId cityId, CountryCode countryCode, string name)
    {
        CityId = cityId;
        CountryCode = countryCode;
        Name = name;
    }

    public static City Create(CityId cityId, CountryCode countryCode, string name)
    {
        ArgumentNullException.ThrowIfNull(cityId);
        ArgumentNullException.ThrowIfNull(countryCode);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        return new City(cityId, countryCode, name);
    }
}
