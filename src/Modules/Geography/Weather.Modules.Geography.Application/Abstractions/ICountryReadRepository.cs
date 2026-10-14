using Weather.BuildingBlocks.Domain.SharedKernel;
using Weather.Modules.Geography.Application.Countries.GetCountries;
using Weather.Modules.Geography.Domain.Countries;

namespace Weather.Modules.Geography.Application.Abstractions;

public interface ICountryReadRepository
{
    /// <summary>Returns countries matching the filter. The filter runs in the database.</summary>
    Task<IReadOnlyList<Country>> GetAsync(CountryQuery query, CancellationToken cancellationToken);

    Task<bool> ExistsAsync(CountryCode countryCode, CancellationToken cancellationToken);
}
