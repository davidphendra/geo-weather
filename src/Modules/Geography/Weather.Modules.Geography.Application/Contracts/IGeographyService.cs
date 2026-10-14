using Weather.Modules.Geography.Application.Cities.GetCitiesByCountry;
using Weather.Modules.Geography.Application.Countries.GetCountries;

namespace Weather.Modules.Geography.Application.Contracts;

/// <summary>The Geography module's public API. Only the host may call this.</summary>
public interface IGeographyService
{
    Task<IReadOnlyList<CountryDto>> GetCountriesAsync(string? search, CancellationToken cancellationToken);

    Task<IReadOnlyList<CityDto>> GetCitiesAsync(string countryCode, CancellationToken cancellationToken);
}
