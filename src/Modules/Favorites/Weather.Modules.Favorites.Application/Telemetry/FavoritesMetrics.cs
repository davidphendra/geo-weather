using Weather.BuildingBlocks.Application.Telemetry;

namespace Weather.Modules.Favorites.Application.Telemetry;

/// <summary>Business KPI gauge: the number of saved favorites (snapshot maintained on startup and on writes).</summary>
internal static class FavoritesMetrics
{
    private static long _savedCount;

    static FavoritesMetrics()
    {
        WeatherMeters.Meter.CreateObservableGauge(
            "favorites.saved.count",
            () => Volatile.Read(ref _savedCount),
            unit: "{favorite}");
    }

    public static void SetSavedCount(long count) => Interlocked.Exchange(ref _savedCount, count);

    public static void Saved() => Interlocked.Increment(ref _savedCount);
}
