using Weather.BuildingBlocks.Domain.SharedKernel;
using Weather.Modules.Geography.Domain;
using Weather.Modules.Geography.Domain.Cities;
using Weather.Modules.Geography.Domain.Countries;

namespace Weather.Modules.Geography.Tests.UnitTests.Domain;

[Trait("Category", "CityDomain")]
public sealed class CityTests
{
    [Fact]
    public void Create_StoresFields()
    {
        CityId id = CityId.New();
        GeoCoordinate coordinate = GeoCoordinate.From(51.5074m, -0.1278m);

        City city = City.Create(id, CountryCode.From("gb"), "London", coordinate);

        Assert.Equal(id, city.Id);
        Assert.Equal("GB", city.CountryCode.Value);
        Assert.Equal("London", city.Name);
        Assert.Equal(coordinate, city.Coordinate);
    }

    [Fact]
    public void CityId_FromEmpty_Throws()
    {
        Assert.Throws<GeographyDomainException>(() => CityId.From(Guid.Empty));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_RejectsEmptyName(string name)
    {
        Assert.ThrowsAny<ArgumentException>(() => City.Create(CityId.New(), CountryCode.From("GB"), name, GeoCoordinate.From(0, 0)));
    }
}
