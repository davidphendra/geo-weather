using global::Weather.Modules.Weather.Application.Contracts;
using global::Weather.Modules.Weather.Application.WeatherReports.GetCurrentWeather;
using global::Weather.WebUI.Api.Common;

using Microsoft.AspNetCore.Mvc;

namespace Weather.WebUI.Api.Modules.Weather;

/// <summary>Current weather for a city.</summary>
[Route("api/weather")]
public sealed class WeatherController(IWeatherService weatherService) : ApiControllerBase
{
    /// <summary>Returns the current weather for a city.</summary>
    [HttpGet("{cityName}")]
    [ProducesResponseType(typeof(WeatherReportDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status502BadGateway)]
    public async Task<ActionResult<WeatherReportDto>> GetCurrentWeatherAsync(
        [FromRoute] string cityName,
        [FromQuery] string? countryCode,
        CancellationToken cancellationToken)
        => Ok(await weatherService.GetCurrentWeatherAsync(cityName, countryCode, cancellationToken));
}
