using Weather.BuildingBlocks.Domain.SharedKernel;
using Weather.Modules.Geography.Domain;
using Weather.Modules.Geography.Domain.Countries;

namespace Weather.Modules.Geography.Tests.UnitTests.Domain;

[Trait("Category", "CountryDomain")]
public sealed class CountryCodeTests
{
    [Theory]
    [InlineData("gb", "GB")]
    [InlineData(" us ", "US")]
    [InlineData("De", "DE")]
    public void From_NormalizesToUpperCase(string input, string expected)
    {
        Assert.Equal(expected, CountryCode.From(input).Value);
    }

    [Theory]
    [InlineData("")]
    [InlineData("  ")]
    [InlineData("GBR")]
    [InlineData("1A")]
    public void From_RejectsInvalidCodes(string input)
    {
        Assert.ThrowsAny<Exception>(() => CountryCode.From(input));
    }

    [Fact]
    public void Equality_IsByValue()
    {
        Assert.Equal(CountryCode.From("gb"), CountryCode.From("GB"));
    }
}
