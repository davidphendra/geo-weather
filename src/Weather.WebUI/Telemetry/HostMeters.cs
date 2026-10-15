using System.Diagnostics;
using System.Diagnostics.Metrics;

using Weather.WebUI.Services;

namespace Weather.WebUI.Telemetry;

/// <summary>
/// Host-level meter for process/build signals (not business metrics). The build-info gauge exposes the
/// running version and commit so telemetry can be grouped by deployment. It is deliberately host-local:
/// the instrument describes the process (like the runtime instrumentation), and
/// <c>Weather.BuildingBlocks.Application</c> must not know the application version.
/// </summary>
internal static class HostMeters
{
    public const string MeterName = "GeoWeather.Host";
    public const string BuildInfoName = "geoweather.build.info";
    public const string VersionTag = "version";
    public const string CommitTag = "commit";

    private static readonly Meter s_meter = new(MeterName, AppVersion.Current.InformationalVersion);

    static HostMeters()
    {
        s_meter.CreateObservableGauge(
            BuildInfoName,
            ObserveBuildInfo,
            unit: "1",
            description: "Build information; the value is always 1; version and commit are attributes.");
    }

    /// <summary>
    /// Forces the static constructor so the gauge is defined. Idempotent: the runtime runs the static
    /// constructor exactly once per process. Call before registering the meter with the SDK.
    /// </summary>
    public static void Initialize() => _ = s_meter;

    private static Measurement<long> ObserveBuildInfo()
    {
        AppVersion version = AppVersion.Current;

        return new Measurement<long>(
            1,
            new TagList
            {
                { VersionTag, version.Version },
                { CommitTag, version.Commit },
            });
    }
}
