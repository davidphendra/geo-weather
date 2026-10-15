using System.Text.Json.Serialization;

namespace Weather.Modules.Weather.Infrastructure.OpenWeatherMap;

/// <summary>Provider-shaped 5 day / 3 hour forecast payload. Never leaves the Infrastructure layer.</summary>
internal sealed class OpenWeatherForecastResponse
{
    [JsonPropertyName("list")] public IReadOnlyList<Step> List { get; init; } = [];
    [JsonPropertyName("city")] public CityBlock City { get; init; } = new();

    internal sealed class Step
    {
        [JsonPropertyName("dt")] public long Dt { get; init; }
        [JsonPropertyName("main")] public MainBlock Main { get; init; } = new();
        [JsonPropertyName("weather")] public IReadOnlyList<WeatherBlock> Weather { get; init; } = [];
        [JsonPropertyName("pop")] public double Pop { get; init; }
    }

    internal sealed class MainBlock
    {
        [JsonPropertyName("temp")] public double Temp { get; init; }
    }

    internal sealed class WeatherBlock
    {
        [JsonPropertyName("main")] public string Main { get; init; } = string.Empty;
    }

    internal sealed class CityBlock
    {
        [JsonPropertyName("name")] public string Name { get; init; } = string.Empty;
        [JsonPropertyName("country")] public string Country { get; init; } = string.Empty;

        /// <summary>Shift in seconds from UTC.</summary>
        [JsonPropertyName("timezone")] public int Timezone { get; init; }
    }
}
