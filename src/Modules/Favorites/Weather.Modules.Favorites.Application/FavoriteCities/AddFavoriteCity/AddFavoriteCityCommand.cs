using MediatR;

using Weather.Modules.Favorites.Application.FavoriteCities.GetFavorites;

namespace Weather.Modules.Favorites.Application.FavoriteCities.AddFavoriteCity;

public sealed record AddFavoriteCityCommand(string CityName, string CountryCode) : IRequest<FavoriteCityDto>;
