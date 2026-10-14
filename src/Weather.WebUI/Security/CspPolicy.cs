namespace Weather.WebUI.Security;

/// <summary>Builds the CSP Level 3 policy header value for a request.</summary>
public static class CspPolicy
{
    /// <summary>Builds the policy. The nonce authorises the Blazor bootstrap script and the import map.</summary>
    public static string Build(string nonce, CspOptions options)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(nonce);
        ArgumentNullException.ThrowIfNull(options);

        string styleSources = string.Join(' ', new[] { "'self'" }.Concat(options.StyleOrigins));
        string fontSources = string.Join(' ', options.FontOrigins);

        return string.Join("; ", new[]
        {
            "base-uri 'self'",
            "default-src 'self'",
            "object-src 'none'",
            $"script-src 'self' 'nonce-{nonce}' 'strict-dynamic'",
            $"script-src-elem 'self' 'nonce-{nonce}' 'strict-dynamic'",
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
