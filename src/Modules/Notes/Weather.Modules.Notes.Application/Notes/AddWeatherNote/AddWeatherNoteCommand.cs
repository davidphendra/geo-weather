using MediatR;

namespace Weather.Modules.Notes.Application.Notes.AddWeatherNote;

public sealed record AddWeatherNoteCommand(
    string CityName,
    string CountryCode,
    string Note,
    decimal? TemperatureCelsius) : IRequest<WeatherNoteDto>;
