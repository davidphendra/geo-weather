using Microsoft.EntityFrameworkCore;

using Weather.Modules.Journal.Application.Abstractions;
using Weather.Modules.Journal.Domain.Cities;
using Weather.Modules.Journal.Domain.FavoriteCities;

namespace Weather.Modules.Journal.Infrastructure.Repositories;

internal sealed class FavoriteCityRepository(JournalContext context) : IFavoriteCityRepository
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
