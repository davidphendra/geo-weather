using Weather.WebUI.Api.ErrorHandling;

namespace Weather.WebUI.Hosting;

/// <summary>
/// Receives browser CSP violation reports. Unauthenticated; the body is capped and only metadata is
/// logged. Bodies are no-cors POSTs, so no CORS policy is required.
/// </summary>
internal static class CspReportEndpoint
{
    private const long MaxReportBytes = 16 * 1024;

    public static IResult Handle(HttpRequest request, ILoggerFactory loggerFactory)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(loggerFactory);

        if (request.ContentLength is > MaxReportBytes)
        {
            // A body (and content type) keeps this response out of the not-found re-execute.
            return Results.Json(
                new { code = ApiErrorCodes.MalformedRequest, detail = "The CSP report exceeds the size limit." },
                statusCode: StatusCodes.Status413PayloadTooLarge);
        }

        loggerFactory.CreateLogger("CspReport").LogWarning(
            "CSP violation reported: contentType={ContentType} length={Length}",
            request.ContentType,
            request.ContentLength);

        return Results.NoContent();
    }
}
