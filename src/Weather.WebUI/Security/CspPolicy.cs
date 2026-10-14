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

        string styleSources = string.Join(' ', SelfSource.Concat(options.StyleOrigins));
        string fontSources = string.Join(' ', options.FontOrigins);
        string scriptSources = $"'self' 'nonce-{nonce}'";

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

            "img-src 'self' data:",
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
