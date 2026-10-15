using System.Net;

using Microsoft.Extensions.Logging;

using Weather.IntegrationTests.Support;

namespace Weather.IntegrationTests.Http;

/// <summary>
/// The host must print its build identity on the console during startup, exactly once, without secrets
/// (e16s02). The startup line is emitted by an <c>IHostedService</c>, so it is observable here.
/// </summary>
[Trait("Category", "StartupLog")]
public sealed class StartupLogTests(StartupLogHostFactory factory) : IClassFixture<StartupLogHostFactory>
{
    [Fact]
    public async Task Startup_prints_one_version_line()
    {
        // Starting the host runs its hosted services, including the startup version logger.
        using HttpResponseMessage response = await factory.CreateClient().GetAsync("/health/live");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        CapturedLog line = Assert.Single(factory.Captured.Logs, log =>
            log.Level == LogLevel.Information && log.Message.Contains("starting in", StringComparison.Ordinal));

        Assert.True(line.Properties.ContainsKey("Version"), "the startup line must carry the version");
        Assert.True(line.Properties.ContainsKey("Commit"), "the startup line must carry the commit");
        Assert.DoesNotContain("ApiKey", line.Message, StringComparison.OrdinalIgnoreCase);
    }
}
