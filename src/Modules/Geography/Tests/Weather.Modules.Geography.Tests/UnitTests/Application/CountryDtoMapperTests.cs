using Weather.BuildingBlocks.Domain.SharedKernel;
using Weather.Modules.Geography.Application.Countries.GetCountries;
using Weather.Modules.Geography.Domain.Countries;

namespace Weather.Modules.Geography.Tests.UnitTests.Application;

[Trait("Category", "CountryDto")]
public sealed class CountryDtoMapperTests
{
    [Fact]
    public void ToDto_MapsCodeAndName()
    {
        Country country = Country.Create(CountryCode.From("gb"), "United Kingdom");

        CountryDto dto = country.ToDto();

        Assert.Equal("GB", dto.Code);
        Assert.Equal("United Kingdom", dto.Name);
    }
}
