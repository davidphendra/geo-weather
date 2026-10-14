using global::Weather.Api.Common;
using global::Weather.Modules.Notes.Application.Contracts;
using global::Weather.Modules.Notes.Application.Notes;

using Microsoft.AspNetCore.Mvc;

namespace Weather.Api.Modules.Notes;

/// <summary>Weather notes.</summary>
[Route("api/weather/notes")]
public sealed class NotesController(INotesModule notesModule) : ApiControllerBase
{
    /// <summary>Saves a note about a city's weather.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(WeatherNoteDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<WeatherNoteDto>> AddNote(
        [FromBody] AddNoteRequest request,
        CancellationToken cancellationToken)
    {
        WeatherNoteDto note = await notesModule.AddNoteAsync(
            request.CityName,
            request.CountryCode,
            request.Note,
            request.TemperatureCelsius,
            cancellationToken);

        return Created($"/api/weather/notes/{note.Id}", note);
    }
}

/// <summary>Request body for saving a weather note.</summary>
public sealed record AddNoteRequest(
    string CityName,
    string CountryCode,
    string Note,
    decimal? TemperatureCelsius);
