using global::Weather.Modules.Weather.Infrastructure.OpenWeatherMap;

using Microsoft.Extensions.Options;

namespace Weather.Modules.Weather.Tests.UnitTests.Infrastructure;

[Trait("Category", "OpenWeatherResilienceOptions")]
public sealed class OpenWeatherResilienceOptionsValidatorTests
{
    private readonly OpenWeatherResilienceOptionsValidator _validator = new();

    [Fact]
    public void TotalTimeoutBelowAttemptTimeout_Fails()
    {
        ValidateOptionsResult result = _validator.Validate(
            null,
            new OpenWeatherResilienceOptions { AttemptTimeoutSeconds = 10, TotalTimeoutSeconds = 9 });

        Assert.True(result.Failed);
        Assert.Contains("TotalTimeoutSeconds", result.FailureMessage);
        Assert.Contains("AttemptTimeoutSeconds", result.FailureMessage);
    }

    [Fact]
    public void TotalTimeoutEqualToAttemptTimeout_Succeeds()
    {
        ValidateOptionsResult result = _validator.Validate(
            null,
            new OpenWeatherResilienceOptions { AttemptTimeoutSeconds = 10, TotalTimeoutSeconds = 10 });

        Assert.True(result.Succeeded);
    }

    [Fact]
    public void TotalTimeoutAboveAttemptTimeout_Succeeds()
    {
        ValidateOptionsResult result = _validator.Validate(
            null,
            new OpenWeatherResilienceOptions { AttemptTimeoutSeconds = 10, TotalTimeoutSeconds = 30 });

        Assert.True(result.Succeeded);
    }
}
