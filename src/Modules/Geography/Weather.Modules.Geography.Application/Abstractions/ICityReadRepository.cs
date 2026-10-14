using Weather.Modules.Geography.Domain.Cities;
using Weather.Modules.Geography.Domain.Countries;

namespace Weather.Modules.Geography.Application.Abstractions;

public interface ICityReadRepository
{
    /// <summary>Returns the cities of one country. The predicate runs in the database.</summary>
    Task<IReadOnlyList<City>> GetByCountryAsync(CountryCode countryCode, CancellationToken cancellationToken);
}
