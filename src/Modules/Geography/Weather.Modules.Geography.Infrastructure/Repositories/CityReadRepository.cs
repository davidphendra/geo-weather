using Microsoft.EntityFrameworkCore;

using Weather.Modules.Geography.Application.Abstractions;
using Weather.Modules.Geography.Domain.Cities;
using Weather.Modules.Geography.Domain.Countries;

namespace Weather.Modules.Geography.Infrastructure.Repositories;

internal sealed class CityReadRepository(GeographyContext context) : ICityReadRepository
{
    public async Task<IReadOnlyList<City>> GetByCountryAsync(CountryCode countryCode, CancellationToken cancellationToken)
        => await context.Cities
            .AsNoTracking()
            .Where(city => city.CountryCode == countryCode)
            .OrderBy(city => city.Name)
            .ToListAsync(cancellationToken);
}
