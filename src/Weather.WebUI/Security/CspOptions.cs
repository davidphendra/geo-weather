using System.Diagnostics.CodeAnalysis;

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
    [SuppressMessage(
        "Design",
        "CA2227:Collection properties should be read only",
        Justification = "Configuration binding requires a settable collection.")]
    public IList<string> StyleOrigins { get; set; } = ["https://fonts.googleapis.com"];

    /// <summary>Font-file origins permitted by font-src (for example the Google Fonts binary host).</summary>
    [SuppressMessage(
        "Design",
        "CA2227:Collection properties should be read only",
        Justification = "Configuration binding requires a settable collection.")]
    public IList<string> FontOrigins { get; set; } = ["https://fonts.gstatic.com"];

    /// <summary>Script origins permitted by script-src (the pinned Leaflet CDN). Also applied to style-src.</summary>
    [SuppressMessage(
        "Design",
        "CA2227:Collection properties should be read only",
        Justification = "Configuration binding requires a settable collection.")]
    public IList<string> ScriptOrigins { get; set; } = ["https://unpkg.com"];

    /// <summary>Image origins permitted by img-src (Leaflet's marker icons and the map tile host).</summary>
    [SuppressMessage(
        "Design",
        "CA2227:Collection properties should be read only",
        Justification = "Configuration binding requires a settable collection.")]
    public IList<string> ImageOrigins { get; set; } = ["https://unpkg.com", "https://tile.openstreetmap.org"];
}
