using global::Weather.Modules.Weather.Infrastructure.OpenWeatherMap;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

using Weather.IntegrationTests.Support;

namespace Weather.IntegrationTests.Http;

/// <summary>The shipped appsettings.json documents the OpenWeatherMap resilience policy (ADR-0015).</summary>
[Trait("Category", "WeatherResilienceConfig")]
public sealed class WeatherResilienceConfigTests(WeatherHostFactory factory) : IClassFixture<WeatherHostFactory>
{
    [Fact]
    public void AppSettings_ExposeTheResilienceSection()
    {
        IConfiguration configuration = factory.Services.GetRequiredService<IConfiguration>();
        IConfigurationSection section = configuration.GetSection(OpenWeatherResilienceOptions.SectionName);

        Assert.True(section.Exists());
        Assert.Contains(section.GetChildren(), child => child.Key == "MaxRetryAttempts");
        Assert.Contains(section.GetChildren(), child => child.Key == "RetryBaseDelaySeconds");
        Assert.Contains(section.GetChildren(), child => child.Key == "AttemptTimeoutSeconds");
        Assert.Contains(section.GetChildren(), child => child.Key == "TotalTimeoutSeconds");
    }

    [Fact]
    public void AppSettings_BindToTheDocumentedDefaults()
    {
        OpenWeatherResilienceOptions options =
            factory.Services.GetRequiredService<IOptions<OpenWeatherResilienceOptions>>().Value;

        Assert.Equal(3, options.MaxRetryAttempts);
        Assert.Equal(1, options.RetryBaseDelaySeconds);
        Assert.Equal(10, options.AttemptTimeoutSeconds);
        Assert.Equal(30, options.TotalTimeoutSeconds);
    }
}
