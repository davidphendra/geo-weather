namespace Weather.Modules.Favorites.Application.FavoriteCities.GetFavorites;

public sealed record FavoriteCityDto(
    Guid Id,
    Guid CityId,
    string City,
    string CountryCode,
    DateTimeOffset CreatedAtUtc);

internal static class FavoriteCityDtoMapper
{
    public static FavoriteCityDto ToDto(this Abstractions.FavoriteCityView view)
    {
        ArgumentNullException.ThrowIfNull(view);

        return new FavoriteCityDto(view.Id, view.CityId, view.CityName, view.CountryCode, view.CreatedAtUtc);
    }
}
