using System.ComponentModel.DataAnnotations;

using global::Weather.Modules.Weather.Infrastructure.OpenWeatherMap;

namespace Weather.Modules.Weather.Tests.UnitTests.Infrastructure;

[Trait("Category", "OpenWeatherResilienceOptions")]
public sealed class OpenWeatherResilienceOptionsTests
{
    [Fact]
    public void Defaults_PreserveTheShippedTimeoutAndRetryBudget()
    {
        var options = new OpenWeatherResilienceOptions();

        Assert.Equal(3, options.MaxRetryAttempts);
        Assert.Equal(1, options.RetryBaseDelaySeconds);
        Assert.Equal(10, options.AttemptTimeoutSeconds);
        Assert.Equal(30, options.TotalTimeoutSeconds);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(11)]
    public void MaxRetryAttempts_OutOfRange_IsInvalid(int value) =>
        Assert.False(IsValid(new OpenWeatherResilienceOptions { MaxRetryAttempts = value }));

    [Theory]
    [InlineData(0)]
    [InlineData(0.0005)]
    [InlineData(31)]
    public void RetryBaseDelaySeconds_OutOfRange_IsInvalid(double value) =>
        Assert.False(IsValid(new OpenWeatherResilienceOptions { RetryBaseDelaySeconds = value }));

    [Theory]
    [InlineData(0)]
    [InlineData(0.05)]
    [InlineData(61)]
    public void AttemptTimeoutSeconds_OutOfRange_IsInvalid(double value) =>
        Assert.False(IsValid(new OpenWeatherResilienceOptions { AttemptTimeoutSeconds = value }));

    [Theory]
    [InlineData(0)]
    [InlineData(0.05)]
    [InlineData(121)]
    public void TotalTimeoutSeconds_OutOfRange_IsInvalid(double value) =>
        Assert.False(IsValid(new OpenWeatherResilienceOptions { TotalTimeoutSeconds = value }));

    [Fact]
    public void Defaults_AreValid() => Assert.True(IsValid(new OpenWeatherResilienceOptions()));

    private static bool IsValid(OpenWeatherResilienceOptions options)
    {
        List<ValidationResult> results = [];

        return Validator.TryValidateObject(options, new ValidationContext(options), results, validateAllProperties: true);
    }
}
