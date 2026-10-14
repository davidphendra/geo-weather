using Weather.Modules.Geography.Domain.Cities;

namespace Weather.Modules.Geography.Application.Cities.GetCitiesByCountry;

public sealed record CityDto(string Id, string Name, string CountryCode, decimal Latitude, decimal Longitude);

internal static class CityDtoMapper
{
    public static CityDto ToDto(this City city)
    {
        ArgumentNullException.ThrowIfNull(city);

        return new CityDto(
            city.Id.Value.ToString("D"),
            city.Name,
            city.CountryCode.Value,
            city.Coordinate.Latitude,
            city.Coordinate.Longitude);
    }
}
