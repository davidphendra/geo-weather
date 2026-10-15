using MediatR;

using Weather.Modules.Journal.Application.FavoriteCities.GetFavorites;

namespace Weather.Modules.Journal.Application.FavoriteCities.AddFavoriteCity;

public sealed record AddFavoriteCityCommand(string CityName, string CountryCode) : IRequest<FavoriteCityDto>;
