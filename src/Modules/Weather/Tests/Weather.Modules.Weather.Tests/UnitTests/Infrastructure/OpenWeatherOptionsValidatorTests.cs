using global::Weather.Modules.Weather.Infrastructure.OpenWeatherMap;

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

using NSubstitute;

namespace Weather.Modules.Weather.Tests.UnitTests.Infrastructure;

[Trait("Category", "OpenWeatherOptions")]
public sealed class OpenWeatherOptionsValidatorTests
{
    [Theory]
    [InlineData("Development", "", true)]
    [InlineData("Development", "key", true)]
    [InlineData("Production", "", false)]
    [InlineData("Production", "key", true)]
    [InlineData("Staging", "", false)]
    [InlineData("Staging", "key", true)]
    public void RequiresTheKeyOutsideDevelopment(string environmentName, string apiKey, bool expectedValid)
    {
        IHostEnvironment environment = Substitute.For<IHostEnvironment>();
        environment.EnvironmentName.Returns(environmentName);

        OpenWeatherOptionsValidator validator = new(environment);

        ValidateOptionsResult result = validator.Validate(null, new OpenWeatherOptions { ApiKey = apiKey });

        Assert.Equal(expectedValid, result.Succeeded);
    }
}
