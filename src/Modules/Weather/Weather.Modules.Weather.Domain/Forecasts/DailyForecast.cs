using Weather.Modules.Weather.Domain.WeatherReports;

namespace Weather.Modules.Weather.Domain.Forecasts;

/// <summary>A single forecast day in the city's local calendar.</summary>
public sealed record DailyForecast
{
    public DateOnly Date { get; }

    public Temperature High { get; }

    public Temperature Low { get; }

    public PrecipitationProbability Precipitation { get; }

    public SkyCondition SkyCondition { get; }

    private DailyForecast(
        DateOnly date,
        Temperature high,
        Temperature low,
        PrecipitationProbability precipitation,
        SkyCondition skyCondition)
    {
        Date = date;
        High = high;
        Low = low;
        Precipitation = precipitation;
        SkyCondition = skyCondition;
    }

    public static DailyForecast Create(
        DateOnly date,
        Temperature high,
        Temperature low,
        PrecipitationProbability precipitation,
        SkyCondition skyCondition)
    {
        ArgumentNullException.ThrowIfNull(high);
        ArgumentNullException.ThrowIfNull(low);
        ArgumentNullException.ThrowIfNull(precipitation);
        ArgumentNullException.ThrowIfNull(skyCondition);

        if (low.Celsius > high.Celsius)
        {
            throw new WeatherDomainException("Daily low cannot exceed the daily high.");
        }

        return new DailyForecast(date, high, low, precipitation, skyCondition);
    }
}
