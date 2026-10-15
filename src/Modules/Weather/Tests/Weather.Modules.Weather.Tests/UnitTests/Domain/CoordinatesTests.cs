using global::Weather.Modules.Weather.Domain;
using global::Weather.Modules.Weather.Domain.WeatherReports;

namespace Weather.Modules.Weather.Tests.UnitTests.Domain;

[Trait("Category", "Coordinates")]
public sealed class CoordinatesTests
{
    [Theory]
    [InlineData(0, 0)]
    [InlineData(90, 180)]
    [InlineData(-90, -180)]
    [InlineData(51.5074, -0.1278)]
    [InlineData(35.6762, 139.6503)]
    public void Of_AcceptsInRangeCoordinates(decimal latitude, decimal longitude)
    {
        Coordinates coordinates = Coordinates.Of(latitude, longitude);

        Assert.Equal(latitude, coordinates.Latitude);
        Assert.Equal(longitude, coordinates.Longitude);
    }

    [Theory]
    [InlineData(90.1)]
    [InlineData(-90.1)]
    [InlineData(120)]
    public void Of_RejectsLatitudeOutOfRange(decimal latitude)
    {
        Assert.Throws<WeatherDomainException>(() => Coordinates.Of(latitude, 0));
    }

    [Theory]
    [InlineData(180.1)]
    [InlineData(-180.1)]
    [InlineData(200)]
    public void Of_RejectsLongitudeOutOfRange(decimal longitude)
    {
        Assert.Throws<WeatherDomainException>(() => Coordinates.Of(0, longitude));
    }
}
