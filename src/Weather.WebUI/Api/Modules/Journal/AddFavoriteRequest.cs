namespace Weather.WebUI.Api.Modules.Journal;

/// <summary>Request body for saving a favorite.</summary>
public sealed record AddFavoriteRequest(string CityName, string CountryCode);
