namespace Weather.WebUI.Services;

/// <summary>Client mirror of the Geography city contract.</summary>
public sealed record CityModel(string Id, string Name, string CountryCode, decimal Latitude, decimal Longitude);
