using System.Net;
using System.Text;

using Weather.IntegrationTests.Support;

namespace Weather.IntegrationTests.Http;

/// <summary>The unauthenticated CSP violation sink (e06s06).</summary>
[Trait("Category", "CspReport")]
public sealed class CspReportTests(WeatherHostFactory factory) : IClassFixture<WeatherHostFactory>
{
    [Fact]
    public async Task Accepts_a_report_and_returns_no_content()
    {
        var content = new StringContent(
            """{"csp-report":{"blocked-uri":"inline","violated-directive":"script-src"}}""",
            Encoding.UTF8,
            "application/csp-report");

        HttpResponseMessage response = await factory.CreateClient().PostAsync("/_csp/report", content);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task Rejects_an_oversized_report()
    {
        var content = new StringContent(new string('a', 17 * 1024), Encoding.UTF8, "application/csp-report");

        HttpResponseMessage response = await factory.CreateClient().PostAsync("/_csp/report", content);

        Assert.Equal(HttpStatusCode.RequestEntityTooLarge, response.StatusCode);
    }
}
