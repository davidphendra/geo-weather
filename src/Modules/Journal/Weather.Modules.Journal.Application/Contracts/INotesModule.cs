using Weather.Modules.Journal.Application.Notes;

namespace Weather.Modules.Journal.Application.Contracts;

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
