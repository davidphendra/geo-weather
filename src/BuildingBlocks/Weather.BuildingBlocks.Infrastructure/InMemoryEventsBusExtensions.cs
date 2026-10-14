using Microsoft.Extensions.DependencyInjection;

using Weather.BuildingBlocks.Application.Events;
using Weather.BuildingBlocks.Infrastructure.Events;

namespace Weather.BuildingBlocks.Infrastructure;

public static class InMemoryEventsBusExtensions
{
    public static IServiceCollection AddInMemoryEventsBus(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddSingleton<IInMemoryEventsBus, InMemoryEventsBus>();

        return services;
    }

    public static IServiceCollection AddIntegrationEventHandler<TEvent, THandler>(this IServiceCollection services)
        where TEvent : IIntegrationEvent
        where THandler : class, IIntegrationEventHandler<TEvent>
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddTransient<IIntegrationEventHandler<TEvent>, THandler>();

        return services;
    }
}
