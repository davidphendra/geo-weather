using global::Weather.Modules.Notes.Application.Contracts;
using global::Weather.Modules.Notes.Application.Notes;
using global::Weather.WebUI.Api.Common;

using Microsoft.AspNetCore.Mvc;

namespace Weather.WebUI.Api.Modules.Notes;

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
    public async Task<ActionResult<WeatherNoteDto>> AddNoteAsync(
        [FromBody] AddNoteRequest request,
        CancellationToken cancellationToken)
    {
        WeatherNoteDto note = await notesModule.AddNoteAsync(
            request.CityName,
            request.CountryCode,
            request.Note,
            request.TemperatureCelsius,
            cancellationToken);

        return Created(new Uri($"/api/weather/notes/{note.Id}", UriKind.Relative), note);
    }
}
