namespace Weather.Api.ErrorHandling;

/// <summary>Stable, machine-readable application error codes (RFC 9457 `code`).</summary>
internal static class ApiErrorCodes
{
    public const string MalformedRequest = "MALFORMED_REQUEST";
    public const string ValidationFailed = "VALIDATION_FAILED";
    public const string ResourceNotFound = "RESOURCE_NOT_FOUND";
    public const string DuplicateResource = "DUPLICATE_RESOURCE";
    public const string DependencyUnavailable = "DEPENDENCY_UNAVAILABLE";
    public const string InternalError = "INTERNAL_ERROR";
}
