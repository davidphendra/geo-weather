using Microsoft.Extensions.DependencyInjection;

using Weather.BuildingBlocks.Application.Events;
using Weather.BuildingBlocks.Infrastructure;

namespace Weather.BuildingBlocks.Tests.Events;

[Trait("Category", "EventsBus")]
public sealed class InMemoryEventsBusTests
{
    private sealed record SampleEvent(string Value) : IIntegrationEvent;

    private sealed class SampleHandler : IIntegrationEventHandler<SampleEvent>
    {
        public List<string> Received { get; } = [];

        public Task HandleAsync(SampleEvent integrationEvent, CancellationToken cancellationToken)
        {
            Received.Add(integrationEvent.Value);

            return Task.CompletedTask;
        }
    }

    [Fact]
    public async Task Publish_InvokesRegisteredHandlers()
    {
        var handler = new SampleHandler();
        ServiceCollection services = new();
        services.AddInMemoryEventsBus();
        services.AddSingleton<IIntegrationEventHandler<SampleEvent>>(handler);

        await using ServiceProvider provider = services.BuildServiceProvider();
        IInMemoryEventsBus bus = provider.GetRequiredService<IInMemoryEventsBus>();

        await bus.PublishAsync(new SampleEvent("city-added"), CancellationToken.None);

        Assert.Equal(["city-added"], handler.Received);
    }

    [Fact]
    public async Task Publish_WithNoHandlers_DoesNotThrow()
    {
        ServiceCollection services = new();
        services.AddInMemoryEventsBus();

        await using ServiceProvider provider = services.BuildServiceProvider();
        IInMemoryEventsBus bus = provider.GetRequiredService<IInMemoryEventsBus>();

        await bus.PublishAsync(new SampleEvent("noop"), CancellationToken.None);
    }
}
