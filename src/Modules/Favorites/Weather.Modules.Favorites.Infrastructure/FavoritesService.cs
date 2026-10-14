using MediatR;

using Weather.Modules.Favorites.Application.Contracts;
using Weather.Modules.Favorites.Application.FavoriteCities.AddFavoriteCity;
using Weather.Modules.Favorites.Application.FavoriteCities.GetFavorites;

namespace Weather.Modules.Favorites.Infrastructure;

internal sealed class FavoritesService(ISender sender) : IFavoritesService
{
    public Task<FavoriteCityDto> AddFavoriteAsync(string cityName, string countryCode, CancellationToken cancellationToken)
        => sender.Send(new AddFavoriteCityCommand(cityName, countryCode), cancellationToken);

    public Task<IReadOnlyList<FavoriteCityDto>> GetFavoritesAsync(CancellationToken cancellationToken)
        => sender.Send(new GetFavoritesQuery(), cancellationToken);
}
