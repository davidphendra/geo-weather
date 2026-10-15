using Weather.Modules.Journal.Application.Abstractions;
using Weather.Modules.Journal.Domain.Notes;

namespace Weather.Modules.Journal.Infrastructure.Repositories;

internal sealed class WeatherNoteRepository(JournalContext context) : IWeatherNoteRepository
{
    public async Task AddAsync(WeatherNote note, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(note);

        await context.WeatherNotes.AddAsync(note, cancellationToken);
    }
}
