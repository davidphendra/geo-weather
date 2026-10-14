using System.Security.Cryptography;

using Microsoft.Extensions.Options;

namespace Weather.WebUI.Security;

/// <summary>
/// Emits a CSP Level 3 header on every response. The per-request nonce is stored in
/// <see cref="HttpContext.Items"/> so the root component can stamp the Blazor scripts.
/// </summary>
public sealed class CspMiddleware(RequestDelegate next, IHostEnvironment environment, IOptions<CspOptions> options)
{
    /// <summary>Key of the per-request nonce in <see cref="HttpContext.Items"/>.</summary>
    public const string NonceItemKey = "csp-nonce";

    public async Task InvokeAsync(HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        // Hex keeps the nonce free of HTML-encoding hazards (+ / =) while remaining a valid CSP nonce token.
        string nonce = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
        context.Items[NonceItemKey] = nonce;

        bool enforce = options.Value.EnforceInDevelopment || !environment.IsDevelopment();
        string headerName = enforce
            ? "Content-Security-Policy"
            : "Content-Security-Policy-Report-Only";

        context.Response.Headers[headerName] = CspPolicy.Build(nonce, options.Value);
        context.Response.Headers["Reporting-Endpoints"] = $"csp-endpoint=\"{options.Value.ReportPath}\"";

        // A nonce must never be replayed from a cache, so HTML responses are made uncacheable.
        context.Response.OnStarting(static state =>
        {
            var responseContext = (HttpContext)state;
            if (responseContext.Response.ContentType?.StartsWith("text/html", StringComparison.OrdinalIgnoreCase) == true)
            {
                responseContext.Response.Headers.CacheControl = "no-store";
            }

            return Task.CompletedTask;
        }, context);

        await next(context);
    }
}
