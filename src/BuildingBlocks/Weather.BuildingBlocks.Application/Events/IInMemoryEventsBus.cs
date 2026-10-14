namespace Weather.BuildingBlocks.Application.Events;

/// <summary>Publishes integration events to their in-process subscribers.</summary>
public interface IInMemoryEventsBus
{
    Task PublishAsync(IIntegrationEvent integrationEvent, CancellationToken cancellationToken);
}
