using Weather.WebUI.Components;

namespace Weather.WebUI.Hosting;

/// <summary>Maps every endpoint: static assets, the REST controllers, the fallbacks and the Blazor app.</summary>
internal static class WebUiEndpointExtensions
{
    public static WebApplication MapWeatherUiEndpoints(this WebApplication app)
    {
        ArgumentNullException.ThrowIfNull(app);

        if (app.Environment.IsDevelopment())
        {
            app.MapOpenApi();
        }

        app.MapStaticAssets();
        app.MapHealthEndpoints();
        app.MapControllers();

        // Unmatched /api routes return RFC 9457 Problem Details with a body, so the not-found
        // re-execute leaves the API surface JSON (BUG-001).
        app.MapFallback("/api/{**slug}", ApiNotFoundEndpoint.Handle);

        // CSP violation sink (unauthenticated). Report bodies are no-cors POSTs; no CORS policy is needed.
        app.MapPost("/_csp/report", CspReportEndpoint.Handle).DisableAntiforgery();

        app.MapRazorComponents<App>()
            .AddInteractiveServerRenderMode(o => o.ContentSecurityFrameAncestorsPolicy = "'none'");

        return app;
    }
}
