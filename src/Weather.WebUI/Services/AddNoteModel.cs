namespace Weather.WebUI.Services;

/// <summary>Request body for saving a weather note.</summary>
public sealed record AddNoteModel(string CityName, string CountryCode, string Note, decimal? TemperatureCelsius);
