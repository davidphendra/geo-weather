namespace Weather.WebUI.Api.ErrorHandling;

/// <summary>Stable problem-type URIs (RFC 9457 `type`).</summary>
internal static class ApiProblemTypes
{
    private const string Base = "https://weather.example.com/problems/";

    public const string MalformedRequest = Base + "malformed-request";
    public const string ValidationError = Base + "validation-error";
    public const string ResourceNotFound = Base + "resource-not-found";
    public const string DuplicateResource = Base + "duplicate-resource";
    public const string DependencyUnavailable = Base + "dependency-unavailable";
    public const string InternalError = Base + "internal-error";
}
