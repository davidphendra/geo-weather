using System.Diagnostics;
using System.Diagnostics.Metrics;

using MediatR;

namespace Weather.BuildingBlocks.Application.Telemetry;

/// <summary>
/// Times every MediatR request and records a span. It is exception-transparent: the original exception is
/// recorded and rethrown unchanged.
/// </summary>
public sealed class TelemetryBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(next);

        string requestName = typeof(TRequest).Name;
        string kind = requestName.EndsWith("Command", StringComparison.Ordinal) ? "command" : "query";
        string module = ResolveModule(typeof(TRequest));

        long start = Stopwatch.GetTimestamp();
        using Activity? activity = WeatherActivity.Source.StartActivity($"mediatr {requestName}");
        activity?.SetTag(WeatherMeters.Tags.RequestName, requestName);
        activity?.SetTag(WeatherMeters.Tags.Module, module);

        string outcome = WeatherMeters.Outcomes.Success;
        try
        {
            return await next(cancellationToken);
        }
        catch
        {
            outcome = WeatherMeters.Outcomes.Error;
            activity?.SetStatus(ActivityStatusCode.Error);
            throw;
        }
        finally
        {
            TagList tags = new()
            {
                { WeatherMeters.Tags.RequestName, requestName },
                { WeatherMeters.Tags.RequestKind, kind },
                { WeatherMeters.Tags.Module, module },
                { WeatherMeters.Tags.MediatROutcome, outcome },
            };

            WeatherMeters.MediatRRequests.Add(1, tags);
            WeatherMeters.MediatRRequestDuration.Record(Stopwatch.GetElapsedTime(start).TotalMilliseconds, tags);
        }
    }

    // "Weather.Modules.Weather.Application.WeatherReports" -> "Weather".
    private static string ResolveModule(Type requestType)
    {
        string[] parts = requestType.Namespace?.Split('.') ?? [];
        int index = Array.IndexOf(parts, "Modules");

        return index >= 0 && index + 1 < parts.Length ? parts[index + 1] : "Unknown";
    }
}
