using Weather.Modules.Favorites.Domain;
using Weather.Modules.Favorites.Domain.Cities;
using Weather.Modules.Favorites.Domain.Countries;

namespace Weather.Modules.Favorites.Tests.UnitTests.Domain;

[Trait("Category", "CityProjectionDomain")]
public sealed class CityProjectionTests
{
    [Fact]
    public void Create_StoresFields()
    {
        CityId cityId = CityId.From(Guid.NewGuid());
        CountryCode code = CountryCode.From("gb");
        GeoCoordinate coordinate = GeoCoordinate.From(51.5074m, -0.1278m);

        City city = City.Create(cityId, code, "London", coordinate);

        Assert.Equal(cityId, city.CityId);
        Assert.Equal("GB", city.CountryCode.Value);
        Assert.Equal("London", city.Name);
        Assert.Equal(coordinate, city.Coordinate);
    }

    [Theory]
    [InlineData("")]
    [InlineData("GBR")]
    [InlineData("1A")]
    public void CountryCode_RejectsInvalid(string value)
    {
        Assert.ThrowsAny<Exception>(() => CountryCode.From(value));
    }

    [Fact]
    public void GeoCoordinate_RejectsOutOfRange()
    {
        Assert.Throws<FavoritesDomainException>(() => GeoCoordinate.From(90.1m, 0));
    }

    [Fact]
    public void CityId_FromEmpty_Throws()
    {
        Assert.Throws<FavoritesDomainException>(() => CityId.From(Guid.Empty));
    }
}
