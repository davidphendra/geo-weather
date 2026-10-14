using Microsoft.Extensions.Options;

namespace Weather.Modules.Weather.Infrastructure.OpenWeatherMap;

/// <summary>
/// Enforces the cross-field invariant that the total request timeout cannot be shorter than a single attempt
/// timeout (ADR-0015). Range checks live on <see cref="OpenWeatherResilienceOptions"/> as data annotations.
/// </summary>
internal sealed class OpenWeatherResilienceOptionsValidator : IValidateOptions<OpenWeatherResilienceOptions>
{
    public ValidateOptionsResult Validate(string? name, OpenWeatherResilienceOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        if (options.TotalTimeoutSeconds < options.AttemptTimeoutSeconds)
        {
            return ValidateOptionsResult.Fail(
                $"OpenWeather:Resilience:TotalTimeoutSeconds ({options.TotalTimeoutSeconds}) must be greater " +
                $"than or equal to AttemptTimeoutSeconds ({options.AttemptTimeoutSeconds}).");
        }

        return ValidateOptionsResult.Success;
    }
}
