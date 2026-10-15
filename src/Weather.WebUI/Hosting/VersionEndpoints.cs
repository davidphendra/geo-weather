using Weather.WebUI.Services;

namespace Weather.WebUI.Hosting;

/// <summary>
/// Host-level build-version endpoint (un-authenticated, like <c>/health/*</c>). Returns the stamped
/// Semantic Version and the source commit so an operator or probe can identify the running artifact.
/// It reads only build metadata, so it cannot fail by design.
/// </summary>
internal static class VersionEndpoints
{
    public static IEndpointRouteBuilder MapVersionEndpoint(this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        endpoints.MapGet("/version", () =>
            {
                AppVersion version = AppVersion.Current;
                return Results.Ok(new VersionResponse(
                    version.Version,
                    version.InformationalVersion,
                    version.Commit));
            })
            .WithName("GetVersion")
            .WithSummary("Returns the running application's build version and source commit.")
            .Produces<VersionResponse>(StatusCodes.Status200OK);

        return endpoints;
    }

    internal sealed record VersionResponse(string Version, string InformationalVersion, string Commit);
}
