using Weather.Modules.Geography.Domain;

namespace Weather.Modules.Geography.Tests.UnitTests.Domain;

[Trait("Category", "CityDomain")]
public sealed class GeoCoordinateTests
{
    [Fact]
    public void From_StoresLatitudeAndLongitude()
    {
        GeoCoordinate coordinate = GeoCoordinate.From(51.5074m, -0.1278m);

        Assert.Equal(51.5074m, coordinate.Latitude);
        Assert.Equal(-0.1278m, coordinate.Longitude);
    }

    [Theory]
    [InlineData(90.1, 0)]
    [InlineData(-90.1, 0)]
    public void From_RejectsLatitudeOutOfRange(double latitude, double longitude)
    {
        Assert.Throws<GeographyDomainException>(() => GeoCoordinate.From((decimal)latitude, (decimal)longitude));
    }

    [Theory]
    [InlineData(0, 180.1)]
    [InlineData(0, -180.1)]
    public void From_RejectsLongitudeOutOfRange(double latitude, double longitude)
    {
        Assert.Throws<GeographyDomainException>(() => GeoCoordinate.From((decimal)latitude, (decimal)longitude));
    }
}
