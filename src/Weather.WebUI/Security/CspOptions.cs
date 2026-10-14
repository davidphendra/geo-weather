namespace Weather.WebUI.Security;

/// <summary>Content Security Policy configuration (bound from the "Csp" section).</summary>
public sealed class CspOptions
{
    public const string SectionName = "Csp";

    /// <summary>When true, the policy is enforced in Development instead of report-only.</summary>
    public bool EnforceInDevelopment { get; set; }

    /// <summary>Violation report sink. A header-only directive such as frame-ancestors is not supported by meta tags.</summary>
    public string ReportPath { get; set; } = "/_csp/report";

    /// <summary>Stylesheet origins permitted by style-src (for example the Google Fonts stylesheet host).</summary>
    public IList<string> StyleOrigins { get; set; } = ["https://fonts.googleapis.com"];

    /// <summary>Font-file origins permitted by font-src (for example the Google Fonts binary host).</summary>
    public IList<string> FontOrigins { get; set; } = ["https://fonts.gstatic.com"];
}
