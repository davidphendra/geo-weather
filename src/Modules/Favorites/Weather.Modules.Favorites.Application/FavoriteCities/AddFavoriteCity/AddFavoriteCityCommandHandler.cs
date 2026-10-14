using MediatR;

using Weather.BuildingBlocks.Application.Abstractions;
using Weather.Modules.Favorites.Application.Abstractions;
using Weather.Modules.Favorites.Application.FavoriteCities.GetFavorites;
using Weather.Modules.Favorites.Domain.Cities;
using Weather.Modules.Favorites.Domain.FavoriteCities;

namespace Weather.Modules.Favorites.Application.FavoriteCities.AddFavoriteCity;

internal sealed class AddFavoriteCityCommandHandler(
    ICityProjectionRepository cityProjections,
    IFavoriteCityRepository favorites,
    IUnitOfWork unitOfWork,
    IDateTimeProvider dateTimeProvider)
    : IRequestHandler<AddFavoriteCityCommand, FavoriteCityDto>
{
    public async Task<FavoriteCityDto> Handle(AddFavoriteCityCommand request, CancellationToken cancellationToken)
    {
        City city = await cityProjections.FindByNameAndCountryAsync(
            request.CityName,
            request.CountryCode,
            cancellationToken)
            ?? throw new CityNotFoundException(request.CityName, request.CountryCode);

        if (await favorites.ExistsByCityIdAsync(city.CityId, cancellationToken))
        {
            throw new FavoriteAlreadyExistsException(city.CityId);
        }

        FavoriteCity favorite = FavoriteCity.Create(FavoriteCityId.New(), city.CityId, dateTimeProvider.UtcNow);

        await favorites.AddAsync(favorite, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new FavoriteCityDto(
            favorite.Id.Value,
            city.CityId.Value,
            city.Name,
            city.CountryCode.Value,
            favorite.CreatedAtUtc);
    }
}
