namespace Weather.WebUI.Security;

/// <summary>Builds the CSP Level 3 policy header value for a request.</summary>
public static class CspPolicy
{
    private static readonly string[] SelfSource = ["'self'"];

    /// <summary>Builds the policy. The nonce authorises the Blazor bootstrap script and the import map.</summary>
    public static string Build(string nonce, CspOptions options)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(nonce);
        ArgumentNullException.ThrowIfNull(options);

        // Leaflet serves both its script and its stylesheet from the same pinned CDN host, so the
        // configured script origins also extend style-src.
        string scriptSources = string.Join(
            ' ',
            SelfSource.Concat([$"'nonce-{nonce}'"]).Concat(options.ScriptOrigins));
        string styleSources = string.Join(' ', SelfSource.Concat(options.StyleOrigins).Concat(options.ScriptOrigins));
        string fontSources = string.Join(' ', options.FontOrigins);
        string imageSources = string.Join(' ', SelfSource.Concat(["data:"]).Concat(options.ImageOrigins));

        return string.Join("; ", new[]
        {
            "base-uri 'self'",
            "default-src 'self'",
            "object-src 'none'",

            // 'strict-dynamic' is deliberately NOT used: Blazor emits colocated .razor.js module
            // scripts as <script src> without a nonce, which 'strict-dynamic' would block.
            $"script-src {scriptSources}",
            $"script-src-elem {scriptSources}",
            "script-src-attr 'none'",

            $"style-src {styleSources}",
            $"style-src-elem {styleSources}",
            "style-src-attr 'none'",
            $"font-src {fontSources}",

            $"img-src {imageSources}",
            "connect-src 'self' ws: wss:",
            "form-action 'self'",
            "frame-ancestors 'none'",
            "manifest-src 'self'",
            "upgrade-insecure-requests",
            "report-to csp-endpoint",
            $"report-uri {options.ReportPath}",
        });
    }
}
