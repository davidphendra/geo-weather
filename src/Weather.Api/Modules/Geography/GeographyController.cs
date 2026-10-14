using global::Weather.Api.Common;
using global::Weather.Modules.Geography.Application.Cities.GetCitiesByCountry;
using global::Weather.Modules.Geography.Application.Contracts;
using global::Weather.Modules.Geography.Application.Countries.GetCountries;

using Microsoft.AspNetCore.Mvc;

namespace Weather.Api.Modules.Geography;

/// <summary>Country and city catalog.</summary>
[Route("api/countries")]
public sealed class GeographyController(IGeographyService geographyService) : ApiControllerBase
{
    /// <summary>Returns the country catalog, optionally filtered by name.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<CountryDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<IReadOnlyList<CountryDto>>> GetCountries(
        [FromQuery] string? search,
        CancellationToken cancellationToken)
    {
        IReadOnlyList<CountryDto> countries = await geographyService.GetCountriesAsync(search, cancellationToken);

        return Ok(countries);
    }

    /// <summary>Returns the cities of one country.</summary>
    [HttpGet("{countryCode}/cities")]
    [ProducesResponseType(typeof(IReadOnlyList<CityDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<IReadOnlyList<CityDto>>> GetCitiesByCountry(
        [FromRoute] string countryCode,
        CancellationToken cancellationToken)
    {
        IReadOnlyList<CityDto> cities = await geographyService.GetCitiesAsync(countryCode, cancellationToken);

        return Ok(cities);
    }
}
