using Weather.WebUI.Api.ErrorHandling;

namespace Weather.WebUI.Hosting;

/// <summary>Terminal handler for an unmatched /api route: RFC 9457 Problem Details instead of HTML.</summary>
internal static class ApiNotFoundEndpoint
{
    public static IResult Handle() => Results.Problem(
        type: ApiProblemTypes.ResourceNotFound,
        title: "Resource not found",
        statusCode: StatusCodes.Status404NotFound,
        detail: "No API endpoint matches the request.",
        extensions: new Dictionary<string, object?> { ["code"] = ApiErrorCodes.ResourceNotFound });
}
