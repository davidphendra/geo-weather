namespace Weather.WebUI.Api.Modules.Favorites;

/// <summary>Request body for saving a favorite.</summary>
public sealed record AddFavoriteRequest(string CityName, string CountryCode);
