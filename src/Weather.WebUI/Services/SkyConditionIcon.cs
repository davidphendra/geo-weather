namespace Weather.WebUI.Services;

/// <summary>Maps a domain sky condition to a Material Symbols icon name.</summary>
internal static class SkyConditionIcon
{
    public static string For(string skyCondition) => skyCondition switch
    {
        "Clear" => "sunny",
        "Partly cloudy" => "partly_cloudy_day",
        "Cloudy" => "cloud",
        "Overcast" => "filter_drama",
        "Rain" or "Drizzle" => "rainy",
        "Thunderstorm" => "thunderstorm",
        "Snow" => "weather_snowy",
        "Fog" => "foggy",
        _ => "cloud",
    };
}
