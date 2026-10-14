using global::Weather.Modules.Weather.Application.WeatherReports.GetCurrentWeather;

namespace Weather.Modules.Weather.Tests.UnitTests.Application;

[Trait("Category", "GetCurrentWeather")]
public sealed class GetCurrentWeatherQueryValidatorTests
{
    private readonly GetCurrentWeatherQueryValidator _validator = new();

    [Fact]
    public void ValidQuery_Passes()
    {
        Assert.True(_validator.Validate(new GetCurrentWeatherQuery("London", "GB")).IsValid);
    }

    [Fact]
    public void EmptyCity_Fails()
    {
        Assert.False(_validator.Validate(new GetCurrentWeatherQuery(string.Empty, "GB")).IsValid);
    }

    [Fact]
    public void OversizedCity_Fails()
    {
        Assert.False(_validator.Validate(new GetCurrentWeatherQuery(new string('x', 101), "GB")).IsValid);
    }

    [Fact]
    public void BadCountryCode_Fails()
    {
        Assert.False(_validator.Validate(new GetCurrentWeatherQuery("London", "GBR")).IsValid);
    }

    [Fact]
    public void NoCountryCode_Passes()
    {
        Assert.True(_validator.Validate(new GetCurrentWeatherQuery("London", null)).IsValid);
    }
}
