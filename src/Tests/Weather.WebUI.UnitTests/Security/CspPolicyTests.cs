using Weather.WebUI.Security;

namespace Weather.WebUI.UnitTests.Security;

/// <summary>CSP Level 3 policy content (e06s06).</summary>
[Trait("Category", "CspPolicy")]
public sealed class CspPolicyTests
{
    private static string Policy() => CspPolicy.Build("NONCE", new CspOptions());

    [Theory]
    [InlineData("base-uri 'self'")]
    [InlineData("default-src 'self'")]
    [InlineData("object-src 'none'")]
    [InlineData("frame-ancestors 'none'")]
    [InlineData("form-action 'self'")]
    [InlineData("report-to csp-endpoint")]
    [InlineData("report-uri /_csp/report")]
    [InlineData("style-src 'self' https://fonts.googleapis.com")]
    [InlineData("style-src-elem 'self' https://fonts.googleapis.com")]
    [InlineData("style-src-attr 'none'")]
    [InlineData("font-src https://fonts.gstatic.com")]
    [InlineData("script-src-attr 'none'")]
    [InlineData("img-src 'self' data:")]
    [InlineData("connect-src 'self' ws: wss:")]
    public void Emits_directive(string directive) => Assert.Contains(directive, Policy());

    [Fact]
    public void Emits_the_nonce_but_not_strict_dynamic()
    {
        string policy = Policy();

        Assert.Contains("'nonce-NONCE'", policy);
        Assert.Contains("script-src 'self' 'nonce-NONCE'", policy);
        // Colocated .razor.js module scripts have no nonce, so 'strict-dynamic' must not be set.
        Assert.DoesNotContain("'strict-dynamic'", policy);
    }

    [Fact]
    public void Does_not_widen_style_src_to_any_https()
    {
        Assert.DoesNotContain("style-src https:", Policy());
        Assert.DoesNotContain("'unsafe-inline'", Policy());
        Assert.DoesNotContain("'unsafe-eval'", Policy());
    }

    [Fact]
    public void Uses_the_configured_report_path()
    {
        string policy = CspPolicy.Build("NONCE", new CspOptions { ReportPath = "/custom-report" });

        Assert.Contains("report-uri /custom-report", policy);
    }
}
