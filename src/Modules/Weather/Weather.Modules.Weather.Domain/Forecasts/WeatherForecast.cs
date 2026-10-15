using Weather.Modules.Weather.Domain.WeatherReports;

namespace Weather.Modules.Weather.Domain.Forecasts;

/// <summary>Daily forecast for a location. Read-only; nothing is persisted.</summary>
public sealed record WeatherForecast
{
    public Location Location { get; }

    public IReadOnlyList<DailyForecast> Days { get; }

    private WeatherForecast(Location location, IReadOnlyList<DailyForecast> days)
    {
        Location = location;
        Days = days;
    }

    /// <summary>
    /// Aggregates three-hour provider intervals into city-local calendar days: high and low from the
    /// day's temperatures, precipitation from the day's maximum, and the most frequent sky condition.
    /// </summary>
    public static WeatherForecast FromIntervals(
        Location location,
        IReadOnlyList<ForecastInterval> intervals,
        TimeSpan utcOffset)
    {
        ArgumentNullException.ThrowIfNull(location);
        ArgumentNullException.ThrowIfNull(intervals);

        if (Math.Abs(utcOffset.TotalHours) > 14)
        {
            throw new WeatherDomainException("UTC offset must be within +/-14 hours.");
        }

        if (intervals.Count == 0)
        {
            throw new WeatherDomainException("A forecast requires at least one interval.");
        }

        DailyForecast[] days =
        [
            .. intervals
                .GroupBy(interval => DateOnly.FromDateTime(interval.AtUtc.ToOffset(utcOffset).DateTime))
                .OrderBy(group => group.Key)
                .Select(group => ToDailyForecast(group.Key, [.. group])),
        ];

        return new WeatherForecast(location, days);
    }

    private static DailyForecast ToDailyForecast(DateOnly date, ForecastInterval[] slots)
    {
        Temperature high = slots[0].Temperature;
        Temperature low = slots[0].Temperature;
        PrecipitationProbability precipitation = slots[0].Precipitation;

        foreach (ForecastInterval slot in slots)
        {
            if (slot.Temperature.Celsius > high.Celsius)
            {
                high = slot.Temperature;
            }

            if (slot.Temperature.Celsius < low.Celsius)
            {
                low = slot.Temperature;
            }

            if (slot.Precipitation.Percent > precipitation.Percent)
            {
                precipitation = slot.Precipitation;
            }
        }

        SkyCondition sky = slots
            .GroupBy(slot => slot.SkyCondition)
            .OrderByDescending(group => group.Count())
            .ThenBy(group => Array.FindIndex(slots, slot => slot.SkyCondition == group.Key))
            .First()
            .Key;

        return DailyForecast.Create(date, high, low, precipitation, sky);
    }
}
