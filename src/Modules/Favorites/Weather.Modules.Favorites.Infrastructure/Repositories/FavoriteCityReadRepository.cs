using Microsoft.EntityFrameworkCore;

using Weather.Modules.Favorites.Application.Abstractions;
using Weather.Modules.Favorites.Domain.Cities;
using Weather.Modules.Favorites.Domain.Countries;
using Weather.Modules.Favorites.Domain.FavoriteCities;

namespace Weather.Modules.Favorites.Infrastructure.Repositories;

internal sealed class FavoriteCityReadRepository(FavoritesContext context) : IFavoriteCityReadRepository
{
    public async Task<IReadOnlyList<FavoriteCityView>> GetFavoritesAsync(CancellationToken cancellationToken)
    {
        // Join + ordering run in SQL; only the needed columns are projected.
        var rows = await context.FavoriteCities
            .AsNoTracking()
            .Join(
                context.CityProjections.AsNoTracking(),
                favorite => favorite.CityId,
                city => city.CityId,
                (favorite, city) => new
                {
                    favorite.Id,
                    favorite.CityId,
                    CityName = city.Name,
                    city.CountryCode,
                    favorite.CreatedAtUtc,
                })
            .OrderBy(row => row.CityName)
            .ToListAsync(cancellationToken);

        return rows
            .Select(row => new FavoriteCityView(
                row.Id.Value,
                row.CityId.Value,
                row.CityName,
                row.CountryCode.Value,
                row.CreatedAtUtc))
            .ToArray();
    }
}
