using Weather.Modules.Journal.Application.FavoriteCities.GetFavorites;

namespace Weather.Modules.Journal.Application.Contracts;

/// <summary>The Favorites module's public API. Only the host may call this.</summary>
public interface IFavoritesService
{
    Task<FavoriteCityDto> AddFavoriteAsync(string cityName, string countryCode, CancellationToken cancellationToken);

    Task<IReadOnlyList<FavoriteCityDto>> GetFavoritesAsync(CancellationToken cancellationToken);
}
