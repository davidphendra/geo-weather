namespace Weather.Modules.Notes.Domain.Notes;

/// <summary>Identity of a weather note.</summary>
public sealed record WeatherNoteId
{
    public Guid Value { get; }

    private WeatherNoteId(Guid value) => Value = value;

    public static WeatherNoteId New() => new(Guid.NewGuid());

    public static WeatherNoteId From(Guid value) => value == Guid.Empty
        ? throw new NotesDomainException("Weather note id cannot be empty.")
        : new WeatherNoteId(value);
}
