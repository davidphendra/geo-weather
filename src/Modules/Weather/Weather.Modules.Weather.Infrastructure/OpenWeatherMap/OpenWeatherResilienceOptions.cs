using System.ComponentModel.DataAnnotations;

namespace Weather.Modules.Weather.Infrastructure.OpenWeatherMap;

/// <summary>
/// Configurable retry resilience for the OpenWeatherMap client, bound from
/// <c>OpenWeather:Resilience</c>. <see cref="MaxRetryAttempts"/> follows Polly semantics: it is the number of
/// retries <em>after</em> the first attempt, so the total number of attempts is <c>MaxRetryAttempts + 1</c>.
/// See specs/adr/0015-openweather-retry-resilience.md.
/// </summary>
public sealed class OpenWeatherResilienceOptions
{
    public const string SectionName = "OpenWeather:Resilience";

    [Range(0, 10)]
    public int MaxRetryAttempts { get; init; } = 3;

    [Range(0.001, 30)]
    public double RetryBaseDelaySeconds { get; init; } = 1;

    [Range(0.1, 60)]
    public double AttemptTimeoutSeconds { get; init; } = 10;

    [Range(0.1, 120)]
    public double TotalTimeoutSeconds { get; init; } = 30;
}
