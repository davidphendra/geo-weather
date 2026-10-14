namespace Weather.WebUI.Api.Modules.Notes;

/// <summary>Request body for saving a weather note.</summary>
public sealed record AddNoteRequest(
    string CityName,
    string CountryCode,
    string Note,
    decimal? TemperatureCelsius);
