using MediatR;

namespace Weather.Modules.Favorites.Application.FavoriteCities.GetFavorites;

public sealed record GetFavoritesQuery : IRequest<IReadOnlyList<FavoriteCityDto>>;
