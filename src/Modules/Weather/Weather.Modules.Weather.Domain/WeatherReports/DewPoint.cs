namespace Weather.Modules.Weather.Domain.WeatherReports;

/// <summary>The temperature at which air becomes saturated. Derived from temperature and humidity.</summary>
public sealed record DewPoint
{
    public decimal Celsius { get; }

    public decimal Fahrenheit => Math.Round((Celsius * 9m / 5m) + 32m, 1, MidpointRounding.AwayFromZero);

    private DewPoint(decimal celsius) => Celsius = Math.Round(celsius, 1, MidpointRounding.AwayFromZero);

    public static DewPoint Derive(Temperature temperature, RelativeHumidity humidity)
    {
        ArgumentNullException.ThrowIfNull(temperature);
        ArgumentNullException.ThrowIfNull(humidity);

        const double a = 17.27;
        const double b = 237.7;
        double tc = (double)temperature.Celsius;
        double alpha = (a * tc / (b + tc)) + Math.Log(humidity.Percent / 100.0);
        decimal derived = (decimal)(b * alpha / (a - alpha));

        // Physical invariant: dew point can never exceed the air temperature.
        return new DewPoint(Math.Min(derived, temperature.Celsius));
    }
}
