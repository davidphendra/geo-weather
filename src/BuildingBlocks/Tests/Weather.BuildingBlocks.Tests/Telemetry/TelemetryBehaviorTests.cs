using System.Diagnostics.Metrics;

using MediatR;

using Weather.BuildingBlocks.Application.Telemetry;

namespace Weather.BuildingBlocks.Tests.Telemetry;

[Trait("Category", "Observability")]
public sealed class TelemetryBehaviorTests
{
    private sealed record SampleQuery : IRequest<string>;

    private sealed record SampleCommand : IRequest<string>;

    [Fact]
    public async Task Success_RecordsARequest()
    {
        long requests = 0;

        using MeterListener listener = new();
        listener.SetMeasurementEventCallback<long>((instrument, value, _, _) =>
        {
            if (instrument.Name == "mediatr.requests")
            {
                Interlocked.Add(ref requests, value);
            }
        });
        listener.EnableMeasurementEvents(WeatherMeters.MediatRRequests);
        listener.Start();

        TelemetryBehavior<SampleQuery, string> behavior = new();

        string result = await behavior.Handle(
            new SampleQuery(),
            _ => Task.FromResult("ok"),
            CancellationToken.None);

        Assert.Equal("ok", result);
        Assert.Equal(1, Interlocked.Read(ref requests));
    }

    [Fact]
    public async Task Failure_IsRethrownUnchanged()
    {
        TelemetryBehavior<SampleCommand, string> behavior = new();

        await Assert.ThrowsAsync<InvalidOperationException>(() => behavior.Handle(
            new SampleCommand(),
            _ => throw new InvalidOperationException("boom"),
            CancellationToken.None));
    }
}
