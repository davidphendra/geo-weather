namespace Weather.WebUI.Services;

/// <summary>Request body for saving a favorite city.</summary>
public sealed record AddFavoriteModel(string CityName, string CountryCode);
