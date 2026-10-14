using Microsoft.EntityFrameworkCore;

using Weather.BuildingBlocks.Domain.SharedKernel;
using Weather.Modules.Notes.Application.Abstractions;
using Weather.Modules.Notes.Domain.Cities;

namespace Weather.Modules.Notes.Infrastructure.Repositories;

internal sealed class CityProjectionRepository(NotesContext context) : ICityProjectionRepository
{
    public async Task<City?> FindByNameAndCountryAsync(
        string cityName,
        string countryCode,
        CancellationToken cancellationToken)
    {
        CountryCode code = CountryCode.From(countryCode);

        return await context.CityProjections
            .AsNoTracking()
            .FirstOrDefaultAsync(city => city.Name == cityName && city.CountryCode == code, cancellationToken);
    }

    public async Task UpsertAsync(City city, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(city);

        bool exists = await context.CityProjections
            .AsNoTracking()
            .AnyAsync(existing => existing.CityId == city.CityId, cancellationToken);

        if (!exists)
        {
            await context.CityProjections.AddAsync(city, cancellationToken);
        }
    }
}
