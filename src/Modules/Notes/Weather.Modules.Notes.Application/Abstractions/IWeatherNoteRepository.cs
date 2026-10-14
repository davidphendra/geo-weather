using Weather.Modules.Notes.Domain.Notes;

namespace Weather.Modules.Notes.Application.Abstractions;

public interface IWeatherNoteRepository
{
    Task AddAsync(WeatherNote note, CancellationToken cancellationToken);
}
