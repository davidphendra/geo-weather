using Weather.BuildingBlocks.Domain.SharedKernel;
using Weather.Modules.Notes.Domain;
using Weather.Modules.Notes.Domain.Cities;

namespace Weather.Modules.Notes.Tests.UnitTests.Domain;

[Trait("Category", "CityProjectionDomain")]
[Trait("Category", "CityProjectionConfig")]
public sealed class CityProjectionTests
{
    [Fact]
    public void Create_StoresFields()
    {
        CityId cityId = CityId.From(Guid.NewGuid());

        City city = City.Create(cityId, CountryCode.From("gb"), "London");

        Assert.Equal(cityId, city.CityId);
        Assert.Equal("GB", city.CountryCode.Value);
        Assert.Equal("London", city.Name);
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
    public void CityId_FromEmpty_Throws()
    {
        Assert.Throws<NotesDomainException>(() => CityId.From(Guid.Empty));
    }
}
