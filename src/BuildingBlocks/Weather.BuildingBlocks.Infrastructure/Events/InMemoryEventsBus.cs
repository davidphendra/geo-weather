using System.Reflection;

using Microsoft.Extensions.DependencyInjection;

using Weather.BuildingBlocks.Application.Events;

namespace Weather.BuildingBlocks.Infrastructure.Events;

internal sealed class InMemoryEventsBus(IServiceScopeFactory scopeFactory) : IInMemoryEventsBus
{
    public async Task PublishAsync(IIntegrationEvent integrationEvent, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(integrationEvent);

        Type eventType = integrationEvent.GetType();
        Type handlerType = typeof(IIntegrationEventHandler<>).MakeGenericType(eventType);
        MethodInfo handleMethod = handlerType.GetMethod(nameof(IIntegrationEventHandler<IIntegrationEvent>.HandleAsync))
            ?? throw new InvalidOperationException($"Handler method was not found on '{handlerType}'.");

        using IServiceScope scope = scopeFactory.CreateScope();

        foreach (object? handler in scope.ServiceProvider.GetServices(handlerType))
        {
            if (handler is not null)
            {
                await (Task)handleMethod.Invoke(handler, [integrationEvent, cancellationToken])!;
            }
        }
    }
}
