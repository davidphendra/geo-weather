using MediatR;

using Weather.Modules.Favorites.Application.Abstractions;

namespace Weather.Modules.Favorites.Application.FavoriteCities.GetFavorites;

internal sealed class GetFavoritesQueryHandler(IFavoriteCityReadRepository readRepository)
    : IRequestHandler<GetFavoritesQuery, IReadOnlyList<FavoriteCityDto>>
{
    public async Task<IReadOnlyList<FavoriteCityDto>> Handle(
        GetFavoritesQuery request,
        CancellationToken cancellationToken)
    {
        IReadOnlyList<FavoriteCityView> views = await readRepository.GetFavoritesAsync(cancellationToken);

        return views.Select(view => view.ToDto()).ToArray();
    }
}
