using Weather.Modules.Notes.Application.Notes;

namespace Weather.Modules.Notes.Application.Contracts;

/// <summary>The Notes module's public API. Only the host may call this.</summary>
public interface INotesModule
{
    Task<WeatherNoteDto> AddNoteAsync(
        string cityName,
        string countryCode,
        string note,
        decimal? temperatureCelsius,
        CancellationToken cancellationToken);
}
