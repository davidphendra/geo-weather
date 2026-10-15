using Microsoft.EntityFrameworkCore;

using Weather.Modules.Geography.Infrastructure;
using Weather.Modules.Journal.Infrastructure;

namespace Weather.WebUI.Hosting;

/// <summary>Liveness and readiness probes consumed by the platform health check and uptime monitoring.</summary>
internal static class HealthEndpoints
{
    public static IEndpointRouteBuilder MapHealthEndpoints(this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        endpoints.MapGet("/health/live", () => Results.Ok(new HealthStatus("healthy")));

        endpoints.MapGet("/health/ready", async (
            IServiceScopeFactory scopeFactory,
            CancellationToken cancellationToken) =>
        {
            using IServiceScope scope = scopeFactory.CreateScope();

            bool ready =
                await CanConnectAsync(scope.ServiceProvider.GetRequiredService<GeographyContext>(), cancellationToken)
                && await CanConnectAsync(scope.ServiceProvider.GetRequiredService<JournalContext>(), cancellationToken);

            return ready
                ? Results.Ok(new HealthStatus("ready"))
                : Results.Json(new HealthStatus("unready"), statusCode: StatusCodes.Status503ServiceUnavailable);
        });

        return endpoints;
    }

    private static Task<bool> CanConnectAsync(DbContext context, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);

        return context.Database.CanConnectAsync(cancellationToken);
    }

    private sealed record HealthStatus(string Status);
}
