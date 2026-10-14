using Weather.Modules.Favorites.Domain.Cities;
using Weather.Modules.Favorites.Domain.FavoriteCities;

namespace Weather.Modules.Favorites.Application.Abstractions;

public interface IFavoriteCityRepository
{
    Task<bool> ExistsByCityIdAsync(CityId cityId, CancellationToken cancellationToken);

    Task AddAsync(FavoriteCity favorite, CancellationToken cancellationToken);
}
