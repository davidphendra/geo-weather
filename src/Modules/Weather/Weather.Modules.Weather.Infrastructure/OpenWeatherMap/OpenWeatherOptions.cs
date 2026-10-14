using System.ComponentModel.DataAnnotations;

namespace Weather.Modules.Weather.Infrastructure.OpenWeatherMap;

public sealed class OpenWeatherOptions
{
    public const string SectionName = "OpenWeather";

    [Required]
    public string BaseUrl { get; init; } = "https://api.openweathermap.org/";

    [Required]
    public string ApiKey { get; init; } = string.Empty;
}
