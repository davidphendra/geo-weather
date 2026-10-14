using System.Diagnostics;
using System.Diagnostics.Metrics;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

using Weather.BuildingBlocks.Application.Telemetry;

namespace Weather.WebUI.Api.ErrorHandling;

/// <summary>Records the shared <c>api.errors</c> counter with the stable code and matched route template.</summary>
internal static class ApiErrorMetrics
{
    public static void Record(string code, int statusCode, HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        string route = (context.GetEndpoint() as RouteEndpoint)?.RoutePattern.RawText
            ?? context.Request.Path.Value
            ?? "unknown";

        WeatherMeters.ApiErrors.Add(1, new TagList
        {
            { WeatherMeters.Tags.ErrorCode, code },
            { WeatherMeters.Tags.StatusCode, statusCode },
            { WeatherMeters.Tags.RequestMethod, context.Request.Method },
            { WeatherMeters.Tags.Route, route },
        });
    }
}
