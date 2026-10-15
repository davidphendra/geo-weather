using System.Diagnostics;
using System.Diagnostics.Metrics;

using MediatR;

using Weather.BuildingBlocks.Application.Abstractions;
using Weather.BuildingBlocks.Application.Telemetry;
using Weather.Modules.Journal.Application.Abstractions;
using Weather.Modules.Journal.Application.Telemetry;
using Weather.Modules.Journal.Domain.Cities;
using Weather.Modules.Journal.Domain.Notes;

namespace Weather.Modules.Journal.Application.Notes.AddWeatherNote;

internal sealed class AddWeatherNoteCommandHandler(
    ICityProjectionRepository cityProjections,
    IWeatherNoteRepository notes,
    IUnitOfWork unitOfWork,
    IDateTimeProvider dateTimeProvider)
    : IRequestHandler<AddWeatherNoteCommand, WeatherNoteDto>
{
    public async Task<WeatherNoteDto> Handle(AddWeatherNoteCommand request, CancellationToken cancellationToken)
    {
        long start = Stopwatch.GetTimestamp();
        string outcome = WeatherMeters.Outcomes.Error;

        try
        {
            City city = await cityProjections.FindByNameAndCountryAsync(
                request.CityName,
                request.CountryCode,
                cancellationToken)
                ?? throw new CityNotFoundException(request.CityName, request.CountryCode);

            WeatherNote note = WeatherNote.Create(
                WeatherNoteId.New(),
                city.Name,
                city.CountryCode,
                request.Note,
                request.TemperatureCelsius,
                dateTimeProvider.UtcNow);

            await notes.AddAsync(note, cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);

            outcome = WeatherMeters.Outcomes.Created;

            return new WeatherNoteDto(
                note.Id.Value,
                note.CityName,
                note.CountryCode.Value,
                note.Note,
                note.TemperatureCelsius,
                note.CreatedAtUtc);
        }
        catch (CityNotFoundException)
        {
            outcome = WeatherMeters.Outcomes.CityNotFound;

            throw;
        }
        finally
        {
            TagList tags = new()
            {
                { WeatherMeters.Tags.Operation, "add" },
                { WeatherMeters.Tags.NotesOutcome, outcome },
            };

            WeatherMeters.NotesOperations.Add(1, tags);
            WeatherMeters.NotesDuration.Record(Stopwatch.GetElapsedTime(start).TotalMilliseconds, tags);

            if (outcome == WeatherMeters.Outcomes.Created)
            {
                NotesMetrics.Saved();
            }
        }
    }
}
