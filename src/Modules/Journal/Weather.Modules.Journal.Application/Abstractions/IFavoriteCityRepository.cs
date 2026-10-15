using Weather.Modules.Journal.Domain.Cities;
using Weather.Modules.Journal.Domain.FavoriteCities;

namespace Weather.Modules.Journal.Application.Abstractions;

public interface IFavoriteCityRepository
{
    Task<bool> ExistsByCityIdAsync(CityId cityId, CancellationToken cancellationToken);

    Task AddAsync(FavoriteCity favorite, CancellationToken cancellationToken);
}
