using Microsoft.EntityFrameworkCore;

using Weather.Modules.Favorites.Application.Abstractions;
using Weather.Modules.Favorites.Domain.Cities;
using Weather.Modules.Favorites.Domain.FavoriteCities;

namespace Weather.Modules.Favorites.Infrastructure.Repositories;

internal sealed class FavoriteCityRepository(FavoritesContext context) : IFavoriteCityRepository
{
    public Task<bool> ExistsByCityIdAsync(CityId cityId, CancellationToken cancellationToken)
        => context.FavoriteCities
            .AsNoTracking()
            .AnyAsync(favorite => favorite.CityId == cityId, cancellationToken);

    public async Task AddAsync(FavoriteCity favorite, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(favorite);

        await context.FavoriteCities.AddAsync(favorite, cancellationToken);
    }
}
