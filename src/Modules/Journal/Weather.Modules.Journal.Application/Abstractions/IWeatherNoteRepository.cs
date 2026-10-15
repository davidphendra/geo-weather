using Weather.Modules.Journal.Domain.Notes;

namespace Weather.Modules.Journal.Application.Abstractions;

public interface IWeatherNoteRepository
{
    Task AddAsync(WeatherNote note, CancellationToken cancellationToken);
}
