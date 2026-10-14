namespace Weather.Modules.Weather.Domain.WeatherReports;

/// <summary>A normalized description of the sky.</summary>
public sealed record SkyCondition
{
    public static readonly SkyCondition Clear = new("Clear");
    public static readonly SkyCondition PartlyCloudy = new("Partly cloudy");
    public static readonly SkyCondition Cloudy = new("Cloudy");
    public static readonly SkyCondition Overcast = new("Overcast");
    public static readonly SkyCondition Rain = new("Rain");
    public static readonly SkyCondition Drizzle = new("Drizzle");
    public static readonly SkyCondition Thunderstorm = new("Thunderstorm");
    public static readonly SkyCondition Snow = new("Snow");
    public static readonly SkyCondition Fog = new("Fog");
    public static readonly SkyCondition Unknown = new("Unknown");

    public string Value { get; }

    private SkyCondition(string value) => Value = value;

    public static SkyCondition FromProvider(string? providerMain) => providerMain switch
    {
        "Clear" => Clear,
        "Clouds" => Cloudy,
        "Rain" => Rain,
        "Drizzle" => Drizzle,
        "Thunderstorm" => Thunderstorm,
        "Snow" => Snow,
        "Mist" or "Fog" or "Haze" => Fog,
        _ => Unknown,
    };
}
