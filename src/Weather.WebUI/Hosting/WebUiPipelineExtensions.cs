using Weather.WebUI.Security;

namespace Weather.WebUI.Hosting;

/// <summary>
/// Builds the request pipeline. The order is deliberate and must not change: the CSP header is applied
/// to every response, API errors get the Problem Details handler, and the not-found re-execute runs in
/// the main pipeline so unmatched UI routes render the not-found page (BUG-001).
/// </summary>
internal static class WebUiPipelineExtensions
{
    public static WebApplication UseWeatherUiPipeline(this WebApplication app)
    {
        ArgumentNullException.ThrowIfNull(app);

        if (!app.Environment.IsDevelopment())
        {
            // Blazor error page for UI requests; API requests get the Problem Details handler below.
            app.UseExceptionHandler("/Error", createScopeForErrors: true);
            app.UseHsts();
        }

        app.UseHttpsRedirection();

        // CSP Level 3 on every response, before static files/endpoints so all pages are covered.
        app.UseCsp();

        // API requests get the RFC 9457 Problem Details handler; UI requests fall through to /Error.
        app.UseWhen(
            context => context.Request.Path.StartsWithSegments("/api", StringComparison.Ordinal),
            api => api.UseExceptionHandler());

        // Re-execute the Blazor not-found page for unmatched UI routes. It must run in the main pipeline:
        // inside UseWhen the branch forks and the re-executed request never reaches the endpoints (BUG-001).
        app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);

        app.UseAntiforgery();

        return app;
    }
}
