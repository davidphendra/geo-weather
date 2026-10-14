using Microsoft.EntityFrameworkCore;

using Weather.Modules.Geography.Application.Abstractions;
using Weather.Modules.Geography.Application.Countries.GetCountries;
using Weather.Modules.Geography.Domain.Countries;

namespace Weather.Modules.Geography.Infrastructure.Repositories;

internal sealed class CountryReadRepository(GeographyContext context) : ICountryReadRepository
{
    public async Task<IReadOnlyList<Country>> GetAsync(CountryQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        IQueryable<Country> countries = context.Countries.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(query.SearchTerm))
        {
            string pattern = $"%{query.SearchTerm.Trim()}%";
            countries = countries.Where(country => EF.Functions.Like(country.Name, pattern));
        }

        return await countries
            .OrderBy(country => country.Name)
            .ToListAsync(cancellationToken);
    }

    public Task<bool> ExistsAsync(CountryCode countryCode, CancellationToken cancellationToken)
        => context.Countries
            .AsNoTracking()
            .AnyAsync(country => country.Code == countryCode, cancellationToken);
}
