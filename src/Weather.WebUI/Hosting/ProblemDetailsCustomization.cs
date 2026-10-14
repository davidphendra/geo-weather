using System.Diagnostics;

using Microsoft.AspNetCore.Mvc;

namespace Weather.WebUI.Hosting;

/// <summary>Adds the correlation trace id to every Problem Details response.</summary>
internal static class ProblemDetailsCustomization
{
    public static void Customize(ProblemDetailsContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        // The W3C trace id (not the span id) so the value matches the exported OTLP trace.
        string traceId = Activity.Current?.TraceId.ToHexString() ?? context.HttpContext.TraceIdentifier;
        context.ProblemDetails.Extensions.TryAdd("traceId", traceId);
        context.HttpContext.Response.Headers["X-Correlation-ID"] = traceId;
    }
}
