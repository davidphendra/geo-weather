namespace Weather.WebUI.Services;

/// <summary>Client mirror of the Notes contract.</summary>
public sealed record WeatherNoteModel(
    Guid Id,
    string CityName,
    string CountryCode,
    string Note,
    decimal? TemperatureCelsius,
    DateTimeOffset CreatedAtUtc);
