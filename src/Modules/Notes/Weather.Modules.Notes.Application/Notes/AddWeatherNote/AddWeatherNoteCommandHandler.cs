using MediatR;

using Weather.BuildingBlocks.Application.Abstractions;
using Weather.Modules.Notes.Application.Abstractions;
using Weather.Modules.Notes.Domain.Cities;
using Weather.Modules.Notes.Domain.Notes;

namespace Weather.Modules.Notes.Application.Notes.AddWeatherNote;

internal sealed class AddWeatherNoteCommandHandler(
    ICityProjectionRepository cityProjections,
    IWeatherNoteRepository notes,
    IUnitOfWork unitOfWork,
    IDateTimeProvider dateTimeProvider)
    : IRequestHandler<AddWeatherNoteCommand, WeatherNoteDto>
{
    public async Task<WeatherNoteDto> Handle(AddWeatherNoteCommand request, CancellationToken cancellationToken)
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

        return new WeatherNoteDto(
            note.Id.Value,
            note.CityName,
            note.CountryCode.Value,
            note.Note,
            note.TemperatureCelsius,
            note.CreatedAtUtc);
    }
}
