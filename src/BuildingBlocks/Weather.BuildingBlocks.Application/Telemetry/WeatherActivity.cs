using System.Diagnostics;

namespace Weather.BuildingBlocks.Application.Telemetry;

/// <summary>
/// Application <see cref="ActivitySource"/>. The host subscribes with
/// <c>AddSource(WeatherActivity.SourceName)</c> so module spans are exported with the rest of the trace.
/// </summary>
public static class WeatherActivity
{
    public const string SourceName = "Weather.Activity";

    public static readonly ActivitySource Source = new(SourceName, "1.0.0");
}
