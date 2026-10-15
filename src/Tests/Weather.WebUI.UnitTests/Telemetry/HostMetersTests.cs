using System.Diagnostics.Metrics;

using Weather.WebUI.Services;
using Weather.WebUI.Telemetry;

namespace Weather.WebUI.UnitTests.Telemetry;

/// <summary>
/// The host must emit its build identity as a metric so telemetry can be grouped by deployment (e16s03).
/// </summary>
[Trait("Category", "HostMeters")]
public sealed class HostMetersTests
{
    [Fact]
    public void Build_info_reports_one_with_version_and_commit()
    {
        HostMeters.Initialize();

        List<(long Value, string? Version, string? Commit)> measurements = [];

        using MeterListener listener = new();
        listener.InstrumentPublished = (instrument, subscribed) =>
        {
            if (instrument.Meter.Name == HostMeters.MeterName)
            {
                subscribed.EnableMeasurementEvents(instrument);
            }
        };
        listener.SetMeasurementEventCallback<long>((instrument, value, tags, _) =>
        {
            if (instrument.Name != HostMeters.BuildInfoName)
            {
                return;
            }

            string? version = null;
            string? commit = null;

            foreach (KeyValuePair<string, object?> tag in tags)
            {
                if (tag.Key == HostMeters.VersionTag)
                {
                    version = tag.Value as string;
                }

                if (tag.Key == HostMeters.CommitTag)
                {
                    commit = tag.Value as string;
                }
            }

            lock (measurements)
            {
                measurements.Add((value, version, commit));
            }
        });
        listener.Start();
        listener.RecordObservableInstruments();

        (long Value, string? Version, string? Commit) measurement = Assert.Single(measurements);

        Assert.Equal(1, measurement.Value);
        Assert.Equal(AppVersion.Current.Version, measurement.Version);
        Assert.Equal(AppVersion.Current.Commit, measurement.Commit);
    }
}
