using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

using NSubstitute;

using Weather.WebUI.Security;

namespace Weather.WebUI.UnitTests.Security;

/// <summary>Enforcement depends on the environment and the explicit override (e06s06).</summary>
[Trait("Category", "CspEnvironment")]
public sealed class CspMiddlewareTests
{
    private static async Task<IHeaderDictionary> RunAsync(string environmentName, bool enforceInDevelopment)
    {
        IHostEnvironment environment = Substitute.For<IHostEnvironment>();
        environment.EnvironmentName.Returns(environmentName);

        var middleware = new CspMiddleware(
            _ => Task.CompletedTask,
            environment,
            Options.Create(new CspOptions { EnforceInDevelopment = enforceInDevelopment, ReportPath = "/_csp/report" }));

        var context = new DefaultHttpContext();
        await middleware.InvokeAsync(context);

        return context.Response.Headers;
    }

    [Fact]
    public async Task Development_without_override_is_report_only()
    {
        IHeaderDictionary headers = await RunAsync("Development", enforceInDevelopment: false);

        Assert.True(headers.ContainsKey("Content-Security-Policy-Report-Only"));
        Assert.False(headers.ContainsKey("Content-Security-Policy"));
    }

    [Fact]
    public async Task Development_with_override_is_enforced()
    {
        IHeaderDictionary headers = await RunAsync("Development", enforceInDevelopment: true);

        Assert.True(headers.ContainsKey("Content-Security-Policy"));
        Assert.False(headers.ContainsKey("Content-Security-Policy-Report-Only"));
    }

    [Fact]
    public async Task Production_is_enforced()
    {
        IHeaderDictionary headers = await RunAsync("Production", enforceInDevelopment: false);

        Assert.True(headers.ContainsKey("Content-Security-Policy"));
        Assert.False(headers.ContainsKey("Content-Security-Policy-Report-Only"));
    }

    [Fact]
    public async Task Publishes_the_reporting_endpoints_header()
    {
        IHeaderDictionary headers = await RunAsync("Production", enforceInDevelopment: false);

        Assert.Equal("csp-endpoint=\"/_csp/report\"", headers["Reporting-Endpoints"].ToString());
    }
}
