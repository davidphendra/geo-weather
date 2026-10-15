using Weather.WebUI.Services;

namespace Weather.WebUI.Hosting;

/// <summary>
/// Prints the build identity to the console once during startup, before the host serves traffic, so the
/// running version is visible in platform logs (App Service log stream, <c>docker logs</c>) even if the
/// process never receives a request. Logs version, commit, and environment only — never secrets.
/// </summary>
internal sealed class StartupVersionLogger(ILogger<StartupVersionLogger> logger, IHostEnvironment environment)
    : IHostedService
{
    public Task StartAsync(CancellationToken cancellationToken)
    {
        AppVersion version = AppVersion.Current;

        logger.LogInformation(
            "GeoWeather {Version} ({Commit}) starting in {Environment}",
            version.Version,
            version.Commit,
            environment.EnvironmentName);

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
