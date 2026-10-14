namespace Weather.Modules.Weather.Domain.WeatherReports;

/// <summary>Atmospheric pressure in hectopascals.</summary>
public sealed record Pressure
{
    public decimal Hectopascals { get; }

    private Pressure(decimal hectopascals) => Hectopascals = hectopascals;

    public static Pressure FromHectopascals(decimal hectopascals) => new(hectopascals);
}
