using Weather.Modules.Weather.Domain.WeatherReports;

namespace Weather.Modules.Weather.Domain.Forecasts;

/// <summary>A provider forecast step (three hours), normalized to the domain.</summary>
public sealed record ForecastInterval
{
    public DateTimeOffset AtUtc { get; }

    public Temperature Temperature { get; }

    public PrecipitationProbability Precipitation { get; }

    public SkyCondition SkyCondition { get; }

    private ForecastInterval(
        DateTimeOffset atUtc,
        Temperature temperature,
        PrecipitationProbability precipitation,
        SkyCondition skyCondition)
    {
        AtUtc = atUtc;
        Temperature = temperature;
        Precipitation = precipitation;
        SkyCondition = skyCondition;
    }

    public static ForecastInterval Create(
        DateTimeOffset atUtc,
        Temperature temperature,
        PrecipitationProbability precipitation,
        SkyCondition skyCondition)
    {
        ArgumentNullException.ThrowIfNull(temperature);
        ArgumentNullException.ThrowIfNull(precipitation);
        ArgumentNullException.ThrowIfNull(skyCondition);

        if (atUtc.Offset != TimeSpan.Zero)
        {
            throw new WeatherDomainException("Forecast interval time must be UTC.");
        }

        return new ForecastInterval(atUtc, temperature, precipitation, skyCondition);
    }
}
