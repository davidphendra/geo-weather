using Weather.Modules.Notes.Application.Abstractions;
using Weather.Modules.Notes.Domain.Notes;

namespace Weather.Modules.Notes.Infrastructure.Repositories;

internal sealed class WeatherNoteRepository(NotesContext context) : IWeatherNoteRepository
{
    public async Task AddAsync(WeatherNote note, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(note);

        await context.WeatherNotes.AddAsync(note, cancellationToken);
    }
}
