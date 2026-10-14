namespace Weather.BuildingBlocks.Application.Events;

/// <summary>Handles an integration event of type <typeparamref name="TEvent"/>.</summary>
public interface IIntegrationEventHandler<in TEvent>
    where TEvent : IIntegrationEvent
{
    Task HandleAsync(TEvent integrationEvent, CancellationToken cancellationToken);
}
