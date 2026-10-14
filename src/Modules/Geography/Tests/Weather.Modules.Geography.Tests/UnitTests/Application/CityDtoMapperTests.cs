using Weather.Modules.Geography.Application.Cities.GetCitiesByCountry;
using Weather.Modules.Geography.Domain;
using Weather.Modules.Geography.Domain.Cities;
using Weather.Modules.Geography.Domain.Countries;

namespace Weather.Modules.Geography.Tests.UnitTests.Application;

[Trait("Category", "CityDto")]
public sealed class CityDtoMapperTests
{
    [Fact]
    public void ToDto_MapsAllFields()
    {
        City city = City.Create(CityId.New(), CountryCode.From("GB"), "London", GeoCoordinate.From(51.5074m, -0.1278m));

        CityDto dto = city.ToDto();

        Assert.Equal("London", dto.Name);
        Assert.Equal("GB", dto.CountryCode);
        Assert.Equal(51.5074m, dto.Latitude);
        Assert.Equal(-0.1278m, dto.Longitude);
        Assert.False(string.IsNullOrWhiteSpace(dto.Id));
    }
}
