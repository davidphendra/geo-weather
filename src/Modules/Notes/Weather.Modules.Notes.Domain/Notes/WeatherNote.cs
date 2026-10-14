using Weather.Modules.Notes.Domain.Countries;

namespace Weather.Modules.Notes.Domain.Notes;

/// <summary>
/// A user-authored note about a city's weather. City data is denormalized (no foreign key), so a note is a
/// self-contained historical record; temperature is stored in Celsius.
/// </summary>
public sealed class WeatherNote
{
    public const int MaxNoteLength = 500;

    public const int MaxCityNameLength = 100;

    public WeatherNoteId Id { get; }

    public string CityName { get; }

    public CountryCode CountryCode { get; }

    public string Note { get; }

    public decimal? TemperatureCelsius { get; }

    public DateTimeOffset CreatedAtUtc { get; }

    private WeatherNote(
        WeatherNoteId id,
        string cityName,
        CountryCode countryCode,
        string note,
        decimal? temperatureCelsius,
        DateTimeOffset createdAtUtc)
    {
        Id = id;
        CityName = cityName;
        CountryCode = countryCode;
        Note = note;
        TemperatureCelsius = temperatureCelsius;
        CreatedAtUtc = createdAtUtc;
    }

    public static WeatherNote Create(
        WeatherNoteId id,
        string cityName,
        CountryCode countryCode,
        string note,
        decimal? temperatureCelsius,
        DateTimeOffset createdAtUtc)
    {
        ArgumentNullException.ThrowIfNull(id);
        ArgumentNullException.ThrowIfNull(countryCode);
        ArgumentException.ThrowIfNullOrWhiteSpace(cityName);
        ArgumentException.ThrowIfNullOrWhiteSpace(note);

        if (cityName.Length > MaxCityNameLength)
        {
            throw new NotesDomainException($"City name cannot exceed {MaxCityNameLength} characters.");
        }

        if (note.Length > MaxNoteLength)
        {
            throw new NotesDomainException($"Note cannot exceed {MaxNoteLength} characters.");
        }

        return new WeatherNote(id, cityName, countryCode, note, temperatureCelsius, createdAtUtc);
    }
}
