using MediatR;

using Weather.Modules.Journal.Application.Contracts;
using Weather.Modules.Journal.Application.Notes;
using Weather.Modules.Journal.Application.Notes.AddWeatherNote;

namespace Weather.Modules.Journal.Infrastructure;

internal sealed class JournalModule(ISender sender) : INotesModule
{
    public Task<WeatherNoteDto> AddNoteAsync(
        string cityName,
        string countryCode,
        string note,
        decimal? temperatureCelsius,
        CancellationToken cancellationToken)
        => sender.Send(new AddWeatherNoteCommand(cityName, countryCode, note, temperatureCelsius), cancellationToken);
}
