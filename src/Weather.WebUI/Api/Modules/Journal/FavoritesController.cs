using global::Weather.Modules.Journal.Application.Contracts;
using global::Weather.Modules.Journal.Application.FavoriteCities.GetFavorites;
using global::Weather.WebUI.Api.Common;

using Microsoft.AspNetCore.Mvc;

namespace Weather.WebUI.Api.Modules.Journal;

/// <summary>Saved cities.</summary>
[Route("api/cities/favorites")]
public sealed class FavoritesController(IFavoritesService favoritesService) : ApiControllerBase
{
    /// <summary>Saves a city to favorites.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(FavoriteCityDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<FavoriteCityDto>> AddFavoriteAsync(
        [FromBody] AddFavoriteRequest request,
        CancellationToken cancellationToken)
    {
        FavoriteCityDto favorite = await favoritesService.AddFavoriteAsync(
            request.CityName,
            request.CountryCode,
            cancellationToken);

        return Created(new Uri($"/api/cities/favorites/{favorite.Id}", UriKind.Relative), favorite);
    }

    /// <summary>Lists the favorites.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<FavoriteCityDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<IReadOnlyList<FavoriteCityDto>>> GetFavoritesAsync(
        CancellationToken cancellationToken)
        => Ok(await favoritesService.GetFavoritesAsync(cancellationToken));
}
