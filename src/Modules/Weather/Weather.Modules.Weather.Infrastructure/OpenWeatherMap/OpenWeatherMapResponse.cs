using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace Weather.Modules.Weather.Infrastructure.OpenWeatherMap;

/// <summary>Provider-shaped payload. Never leaves the Infrastructure layer.</summary>
internal sealed class OpenWeatherMapResponse
{
    [JsonPropertyName("name")] public string Name { get; init; } = string.Empty;
    [JsonPropertyName("dt")] public long Dt { get; init; }
    [JsonPropertyName("visibility")] public int Visibility { get; init; }
    [JsonPropertyName("coord")] public CoordBlock Coord { get; init; } = new();
    [JsonPropertyName("main")] public MainBlock Main { get; init; } = new();
    [JsonPropertyName("wind")] public WindBlock Wind { get; init; } = new();
    [JsonPropertyName("sys")] public SysBlock Sys { get; init; } = new();
    [JsonPropertyName("weather")] public IReadOnlyList<WeatherBlock> Weather { get; init; } = [];

    internal sealed class CoordBlock
    {
        [JsonPropertyName("lat")] public double Lat { get; init; }
        [JsonPropertyName("lon")] public double Lon { get; init; }
    }

    internal sealed class MainBlock
    {
        [JsonPropertyName("temp")] public double Temp { get; init; }
        [JsonPropertyName("pressure")] public double Pressure { get; init; }
        [JsonPropertyName("humidity")] public int Humidity { get; init; }
    }

    internal sealed class WindBlock
    {
        [JsonPropertyName("speed")] public double Speed { get; init; }
        [JsonPropertyName("deg")] public int? Deg { get; init; }
    }

    internal sealed class SysBlock
    {
        [JsonPropertyName("country")] public string Country { get; init; } = string.Empty;
    }

    internal sealed class WeatherBlock
    {
        [JsonPropertyName("main")] public string Main { get; init; } = string.Empty;
    }
}
