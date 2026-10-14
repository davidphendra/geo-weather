namespace Weather.Modules.Notes.Application.Notes;

/// <summary>The created weather note, returned to the API.</summary>
public sealed record WeatherNoteDto(
    Guid Id,
    string CityName,
    string CountryCode,
    string Note,
    decimal? TemperatureCelsius,
    DateTimeOffset CreatedAtUtc);
