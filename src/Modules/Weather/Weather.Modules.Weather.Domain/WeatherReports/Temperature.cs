namespace Weather.Modules.Weather.Domain.WeatherReports;

/// <summary>A temperature value. Stored in Celsius; Fahrenheit is derived.</summary>
public sealed record Temperature
{
    public decimal Celsius { get; }

    public decimal Fahrenheit => Math.Round((Celsius * 9m / 5m) + 32m, 1, MidpointRounding.AwayFromZero);

    private Temperature(decimal celsius) => Celsius = Math.Round(celsius, 1, MidpointRounding.AwayFromZero);

    public static Temperature FromCelsius(decimal celsius) => new(celsius);

    public static Temperature FromFahrenheit(decimal fahrenheit) => new((fahrenheit - 32m) * 5m / 9m);
}
