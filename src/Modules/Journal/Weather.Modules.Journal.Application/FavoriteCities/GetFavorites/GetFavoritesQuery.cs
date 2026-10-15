using MediatR;

namespace Weather.Modules.Journal.Application.FavoriteCities.GetFavorites;

public sealed record GetFavoritesQuery : IRequest<IReadOnlyList<FavoriteCityDto>>;
