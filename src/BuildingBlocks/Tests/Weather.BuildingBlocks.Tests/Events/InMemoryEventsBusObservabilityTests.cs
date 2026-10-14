using System.Diagnostics.Metrics;

using Microsoft.Extensions.DependencyInjection;

using Weather.BuildingBlocks.Application.Events;
using Weather.BuildingBlocks.Application.Telemetry;
using Weather.BuildingBlocks.Infrastructure;

namespace Weather.BuildingBlocks.Tests.Events;

[Trait("Category", "Observability")]
public sealed class InMemoryEventsBusObservabilityTests
{
    private sealed record SampleEvent(string Value) : IIntegrationEvent;

    private sealed class OkHandler : IIntegrationEventHandler<SampleEvent>
    {
        public Task HandleAsync(SampleEvent integrationEvent, CancellationToken cancellationToken) =>
            Task.CompletedTask;
    }

    private sealed class FailingHandler : IIntegrationEventHandler<SampleEvent>
    {
        public Task HandleAsync(SampleEvent integrationEvent, CancellationToken cancellationToken) =>
            throw new InvalidOperationException("handler failed");
    }

    [Fact]
    public async Task HandledEvent_IsCounted()
    {
        long handled = 0;

        using MeterListener listener = new();
        listener.SetMeasurementEventCallback<long>((instrument, value, _, _) =>
        {
            if (instrument.Name == "integration.event.handled")
            {
                Interlocked.Add(ref handled, value);
            }
        });
        listener.EnableMeasurementEvents(WeatherMeters.IntegrationEventsHandled);
        listener.Start();

        await using ServiceProvider provider = BuildProvider<OkHandler>();

        await provider.GetRequiredService<IInMemoryEventsBus>()
            .PublishAsync(new SampleEvent("city-added"), CancellationToken.None);

        Assert.Equal(1, Interlocked.Read(ref handled));
    }

    [Fact]
    public async Task FailingHandler_IsCountedAndRethrown()
    {
        long failed = 0;

        using MeterListener listener = new();
        listener.SetMeasurementEventCallback<long>((instrument, value, _, _) =>
        {
            if (instrument.Name == "integration.event.failed")
            {
                Interlocked.Add(ref failed, value);
            }
        });
        listener.EnableMeasurementEvents(WeatherMeters.IntegrationEventsFailed);
        listener.Start();

        await using ServiceProvider provider = BuildProvider<FailingHandler>();

        await Assert.ThrowsAnyAsync<Exception>(() =>
            provider.GetRequiredService<IInMemoryEventsBus>()
                .PublishAsync(new SampleEvent("city-added"), CancellationToken.None));

        Assert.Equal(1, Interlocked.Read(ref failed));
    }

    private static ServiceProvider BuildProvider<THandler>()
        where THandler : class, IIntegrationEventHandler<SampleEvent>
    {
        ServiceCollection services = new();
        services.AddInMemoryEventsBus();
        services.AddSingleton<IIntegrationEventHandler<SampleEvent>, THandler>();

        return services.BuildServiceProvider();
    }
}
