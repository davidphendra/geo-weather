using MediatR;

using Weather.Modules.Notes.Application.Contracts;
using Weather.Modules.Notes.Application.Notes;
using Weather.Modules.Notes.Application.Notes.AddWeatherNote;

namespace Weather.Modules.Notes.Infrastructure;

internal sealed class NotesModule(ISender sender) : INotesModule
{
    public Task<WeatherNoteDto> AddNoteAsync(
        string cityName,
        string countryCode,
        string note,
        decimal? temperatureCelsius,
        CancellationToken cancellationToken)
        => sender.Send(new AddWeatherNoteCommand(cityName, countryCode, note, temperatureCelsius), cancellationToken);
}
