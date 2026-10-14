using Weather.BuildingBlocks.Application.Telemetry;

namespace Weather.Modules.Notes.Application.Telemetry;

/// <summary>Business KPI gauge: the number of saved notes (snapshot maintained on startup and on writes).</summary>
internal static class NotesMetrics
{
    private static long _savedCount;

    static NotesMetrics()
    {
        WeatherMeters.Meter.CreateObservableGauge(
            "notes.saved.count",
            () => Volatile.Read(ref _savedCount),
            unit: "{note}");
    }

    public static void SetSavedCount(long count) => Interlocked.Exchange(ref _savedCount, count);

    public static void Saved() => Interlocked.Increment(ref _savedCount);
}
