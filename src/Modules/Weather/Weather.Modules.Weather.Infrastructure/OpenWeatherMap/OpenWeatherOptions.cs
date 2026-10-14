using System.ComponentModel.DataAnnotations;

namespace Weather.Modules.Weather.Infrastructure.OpenWeatherMap;

public sealed class OpenWeatherOptions
{
    public const string SectionName = "OpenWeather";

    [Required]
    public Uri BaseUrl { get; init; } = new("https://api.openweathermap.org/");

    [Required]
    public string ApiKey { get; init; } = string.Empty;
}
