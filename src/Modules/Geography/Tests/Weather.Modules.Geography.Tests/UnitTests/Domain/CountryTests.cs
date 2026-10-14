using Weather.Modules.Geography.Domain.Countries;

namespace Weather.Modules.Geography.Tests.UnitTests.Domain;

[Trait("Category", "CountryDomain")]
public sealed class CountryTests
{
    [Fact]
    public void Create_StoresCodeAndName()
    {
        Country country = Country.Create(CountryCode.From("gb"), "United Kingdom");

        Assert.Equal("GB", country.Code.Value);
        Assert.Equal("United Kingdom", country.Name);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_RejectsEmptyName(string name)
    {
        Assert.ThrowsAny<ArgumentException>(() => Country.Create(CountryCode.From("GB"), name));
    }

    [Fact]
    public void Create_RejectsNullCode()
    {
        Assert.Throws<ArgumentNullException>(() => Country.Create(null!, "Nowhere"));
    }
}
