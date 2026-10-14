namespace Weather.Modules.Weather.Domain.WeatherReports;

/// <summary>Relative humidity as a percentage between 0 and 100.</summary>
public sealed record RelativeHumidity
{
    public int Percent { get; }

    private RelativeHumidity(int percent) => Percent = percent;

    public static RelativeHumidity FromPercent(int percent)
    {
        if (percent is < 0 or > 100)
        {
            throw new WeatherDomainException("Relative humidity must be between 0 and 100.");
        }

        return new RelativeHumidity(percent);
    }
}
