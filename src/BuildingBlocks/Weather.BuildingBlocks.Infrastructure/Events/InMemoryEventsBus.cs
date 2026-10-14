using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Reflection;

using Microsoft.Extensions.DependencyInjection;

using Weather.BuildingBlocks.Application.Events;
using Weather.BuildingBlocks.Application.Telemetry;

namespace Weather.BuildingBlocks.Infrastructure.Events;

internal sealed class InMemoryEventsBus(IServiceScopeFactory scopeFactory) : IInMemoryEventsBus
{
    public async Task PublishAsync(IIntegrationEvent integrationEvent, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(integrationEvent);

        Type eventType = integrationEvent.GetType();
        string eventName = eventType.Name;

        using Activity? publish = WeatherActivity.Source.StartActivity($"integration.publish {eventName}");
        publish?.SetTag(WeatherMeters.Tags.EventType, eventName);

        WeatherMeters.IntegrationEventsPublished.Add(1, EventTag(eventName));

        Type handlerType = typeof(IIntegrationEventHandler<>).MakeGenericType(eventType);
        MethodInfo handleMethod = handlerType.GetMethod(nameof(IIntegrationEventHandler<IIntegrationEvent>.HandleAsync))
            ?? throw new InvalidOperationException($"Handler method was not found on '{handlerType}'.");

        using IServiceScope scope = scopeFactory.CreateScope();

        foreach (object? handler in scope.ServiceProvider.GetServices(handlerType))
        {
            if (handler is not null)
            {
                await InvokeHandlerAsync(handler, handleMethod, integrationEvent, eventName, cancellationToken);
            }
        }
    }

    private static async Task InvokeHandlerAsync(
        object handler,
        MethodInfo handleMethod,
        IIntegrationEvent integrationEvent,
        string eventName,
        CancellationToken cancellationToken)
    {
        string handlerModule = handler.GetType().Assembly.GetName().Name ?? "Unknown";
        long start = Stopwatch.GetTimestamp();

        using Activity? activity = WeatherActivity.Source.StartActivity($"integration.handle {eventName}");
        activity?.SetTag(WeatherMeters.Tags.EventType, eventName);
        activity?.SetTag(WeatherMeters.Tags.HandlerModule, handlerModule);

        try
        {
            await (Task)handleMethod.Invoke(handler, [integrationEvent, cancellationToken])!;

            WeatherMeters.IntegrationEventsHandled.Add(1, HandlerTags(eventName, handlerModule));
        }
        catch
        {
            activity?.SetStatus(ActivityStatusCode.Error);
            WeatherMeters.IntegrationEventsFailed.Add(1, HandlerTags(eventName, handlerModule));

            throw;
        }
        finally
        {
            WeatherMeters.IntegrationEventDuration.Record(
                Stopwatch.GetElapsedTime(start).TotalMilliseconds,
                HandlerTags(eventName, handlerModule));
        }
    }

    private static TagList EventTag(string eventName) =>
        new() { { WeatherMeters.Tags.EventType, eventName } };

    private static TagList HandlerTags(string eventName, string handlerModule) => new()
    {
        { WeatherMeters.Tags.EventType, eventName },
        { WeatherMeters.Tags.HandlerModule, handlerModule },
    };
}
