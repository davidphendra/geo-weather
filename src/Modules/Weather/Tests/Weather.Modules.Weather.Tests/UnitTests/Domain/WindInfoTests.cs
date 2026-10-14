using global::Weather.Modules.Weather.Domain;
using global::Weather.Modules.Weather.Domain.WeatherReports;

namespace Weather.Modules.Weather.Tests.UnitTests.Domain;

[Trait("Category", "WindInfo")]
public sealed class WindInfoTests
{
    [Theory]
    [InlineData(0, "N")]
    [InlineData(90, "E")]
    [InlineData(180, "S")]
    [InlineData(270, "W")]
    [InlineData(360, "N")]
    public void Cardinal_MapsDegrees(int degrees, string expected)
    {
        Assert.Equal(expected, WindInfo.FromMetersPerSecond(1m, degrees).Cardinal);
    }

    [Fact]
    public void MissingDirection_IsVariable()
    {
        WindInfo wind = WindInfo.FromMetersPerSecond(3.5m, null);

        Assert.Null(wind.DirectionDegrees);
        Assert.Equal("Variable", wind.Cardinal);
    }

    [Fact]
    public void NegativeSpeed_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => WindInfo.FromMetersPerSecond(-1m, 90));
    }

    [Fact]
    public void OutOfRangeDirection_Throws()
    {
        Assert.Throws<WeatherDomainException>(() => WindInfo.FromMetersPerSecond(1m, 361));
    }
}
