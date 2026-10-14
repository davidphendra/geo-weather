using System.ComponentModel.DataAnnotations;

namespace Weather.Modules.Weather.Infrastructure.OpenWeatherMap;

public sealed class OpenWeatherOptions
{
    public const string SectionName = "OpenWeather";

    [Required]
    public Uri BaseUrl { get; init; } = new("https://api.openweathermap.org/");

    // Never committed. Required outside Development — enforced by OpenWeatherOptionsValidator (SECURITY_PLAN).
    public string ApiKey { get; init; } = string.Empty;
}
