using Microsoft.AspNetCore.Mvc;

using Weather.WebUI.Api.ErrorHandling;

namespace Weather.WebUI.Hosting;

/// <summary>Turns an unparsable request body into a MALFORMED_REQUEST Problem Details response.</summary>
internal static class ApiBehaviorCustomization
{
    public static IActionResult InvalidModelStateResponse(ActionContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        ValidationProblemDetails problem = new(context.ModelState)
        {
            Type = ApiProblemTypes.MalformedRequest,
            Title = "Malformed request",
            Status = StatusCodes.Status400BadRequest,
            Detail = "The request could not be parsed.",
        };
        problem.Extensions["code"] = ApiErrorCodes.MalformedRequest;

        return new BadRequestObjectResult(problem);
    }
}
