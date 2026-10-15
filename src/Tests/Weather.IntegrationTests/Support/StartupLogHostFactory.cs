using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;

namespace Weather.IntegrationTests.Support;

/// <summary>A host that also captures log records, so the startup version line can be asserted (e16s02).</summary>
public sealed class StartupLogHostFactory : WeatherHostFactory
{
    internal CapturingLoggerProvider Captured { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder);
        builder.ConfigureLogging(logging =>
        {
            logging.SetMinimumLevel(LogLevel.Information);
            logging.AddProvider(Captured);
        });
    }
}
