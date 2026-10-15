using global::Weather.Modules.Weather.Application.WeatherReports.GetWeatherForecast;

namespace Weather.Modules.Weather.Tests.UnitTests.Application;

[Trait("Category", "GetWeatherForecast")]
public sealed class GetWeatherForecastQueryValidatorTests
{
    private readonly GetWeatherForecastQueryValidator _validator = new();

    [Fact]
    public void ValidQuery_Passes()
    {
        Assert.True(_validator.Validate(new GetWeatherForecastQuery("London", "GB")).IsValid);
    }

    [Fact]
    public void EmptyCity_Fails()
    {
        Assert.False(_validator.Validate(new GetWeatherForecastQuery(string.Empty, "GB")).IsValid);
    }

    [Fact]
    public void OversizedCity_Fails()
    {
        Assert.False(_validator.Validate(new GetWeatherForecastQuery(new string('x', 101), "GB")).IsValid);
    }

    [Fact]
    public void BadCountryCode_Fails()
    {
        Assert.False(_validator.Validate(new GetWeatherForecastQuery("London", "GBR")).IsValid);
    }

    [Fact]
    public void NoCountryCode_Passes()
    {
        Assert.True(_validator.Validate(new GetWeatherForecastQuery("London", null)).IsValid);
    }
}
