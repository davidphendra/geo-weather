using System.Diagnostics.Metrics;

using Weather.BuildingBlocks.Application.Telemetry;

namespace Weather.BuildingBlocks.Tests.Telemetry;

[Trait("Category", "Observability")]
public sealed class WeatherMetersTests
{
    [Fact]
    public void MediatRRequests_AreObservableByAMeterListener()
    {
        long total = 0;

        using MeterListener listener = new();
        listener.SetMeasurementEventCallback<long>((instrument, value, _, _) =>
        {
            if (instrument.Name == "mediatr.requests")
            {
                Interlocked.Add(ref total, value);
            }
        });
        listener.EnableMeasurementEvents(WeatherMeters.MediatRRequests);
        listener.Start();

        WeatherMeters.MediatRRequests.Add(1);

        Assert.Equal(1, Interlocked.Read(ref total));
    }
}
