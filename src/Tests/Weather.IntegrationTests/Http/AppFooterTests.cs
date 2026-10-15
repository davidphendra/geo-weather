using System.Runtime.InteropServices;
using System.Text.RegularExpressions;

using Weather.IntegrationTests.Support;

namespace Weather.IntegrationTests.Http;

/// <summary>
/// The rendered shell must present the build version to a user before the copyright, and must not repeat
/// the framework description that was removed from the footer (e16s02).
/// </summary>
[Trait("Category", "AppFooter")]
public sealed partial class AppFooterTests(WeatherHostFactory factory) : IClassFixture<WeatherHostFactory>
{
    [Fact]
    public async Task Footer_shows_version_before_copyright_and_drops_the_framework_line()
    {
        string html = await factory.CreateClient().GetStringAsync("/");

        int footerStart = html.IndexOf("<footer", StringComparison.Ordinal);
        int footerEnd = html.IndexOf("</footer>", StringComparison.Ordinal);
        Assert.True(footerStart >= 0 && footerEnd > footerStart, "the page must render a footer");

        string footer = html[footerStart..footerEnd];

        Match version = SemanticVersion().Match(footer);
        Assert.True(version.Success, "the footer must show a semantic version");

        int copyright = footer.IndexOf("GeoWeather", version.Index, StringComparison.Ordinal);
        Assert.True(copyright > version.Index, "the version must appear before the copyright");

        Assert.DoesNotContain(RuntimeInformation.FrameworkDescription, footer, StringComparison.Ordinal);
        Assert.Contains("Kestrel WebHost", footer, StringComparison.Ordinal);
    }

    [GeneratedRegex(@"v\d+\.\d+\.\d+[0-9A-Za-z.+-]*")]
    private static partial Regex SemanticVersion();
}
