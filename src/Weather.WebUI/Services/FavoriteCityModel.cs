namespace Weather.WebUI.Services;

/// <summary>Client mirror of the Favorites contract.</summary>
public sealed record FavoriteCityModel(
    Guid Id,
    Guid CityId,
    string City,
    string CountryCode,
    DateTimeOffset CreatedAtUtc);
