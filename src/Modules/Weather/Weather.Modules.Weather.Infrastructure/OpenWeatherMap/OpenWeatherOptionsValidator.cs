using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace Weather.Modules.Weather.Infrastructure.OpenWeatherMap;

/// <summary>
/// Requires <see cref="OpenWeatherOptions.ApiKey"/> outside Development. In Development the host starts and
/// weather calls degrade (502 DEPENDENCY_UNAVAILABLE) so UI work does not need a key; a real deployment fails
/// fast instead of silently returning errors. See specs/tech-architecture/SECURITY_PLAN_LATEST.md.
/// </summary>
internal sealed class OpenWeatherOptionsValidator(IHostEnvironment environment)
    : IValidateOptions<OpenWeatherOptions>
{
    public ValidateOptionsResult Validate(string? name, OpenWeatherOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        if (environment.IsDevelopment() || !string.IsNullOrWhiteSpace(options.ApiKey))
        {
            return ValidateOptionsResult.Success;
        }

        return ValidateOptionsResult.Fail(
            "OpenWeather:ApiKey is required outside Development. Provide it with `dotnet user-secrets` " +
            "(Development), the OpenWeather__ApiKey environment variable, or a Key Vault reference when deployed.");
    }
}
