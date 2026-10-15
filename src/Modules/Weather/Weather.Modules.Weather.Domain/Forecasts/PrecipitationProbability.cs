namespace Weather.Modules.Weather.Domain.Forecasts;

/// <summary>Chance of precipitation for a forecast period, between 0 and 100 percent.</summary>
public sealed record PrecipitationProbability
{
    public int Percent { get; }

    private PrecipitationProbability(int percent) => Percent = percent;

    public static PrecipitationProbability FromPercent(int percent)
    {
        if (percent is < 0 or > 100)
        {
            throw new WeatherDomainException("Precipitation probability must be between 0 and 100.");
        }

        return new PrecipitationProbability(percent);
    }

    /// <param name="ratio">Provider value between 0 and 1.</param>
    public static PrecipitationProbability FromRatio(double ratio)
    {
        if (double.IsNaN(ratio) || ratio < 0d || ratio > 1d)
        {
            throw new WeatherDomainException("Precipitation probability ratio must be between 0 and 1.");
        }

        return FromPercent((int)Math.Round(ratio * 100d, MidpointRounding.AwayFromZero));
    }
}
