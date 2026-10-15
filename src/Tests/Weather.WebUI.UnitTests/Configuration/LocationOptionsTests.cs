using Microsoft.Extensions.Configuration;

using Weather.WebUI.Configuration;

namespace Weather.WebUI.UnitTests.Configuration;

/// <summary>The default location is bound from the "Location" configuration section.</summary>
[Trait("Category", "LocationOptions")]
public sealed class LocationOptionsTests
{
    [Fact]
    public void Defaults_are_Indonesia_and_Jakarta()
    {
        var options = new LocationOptions();

        Assert.Equal("ID", options.DefaultCountryCode);
        Assert.Equal("Jakarta", options.DefaultCityName);
    }

    [Fact]
    public void Binds_overrides_from_the_Location_section()
    {
        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Location:DefaultCountryCode"] = "GB",
                ["Location:DefaultCityName"] = "London",
            })
            .Build();

        LocationOptions options = configuration
            .GetSection(LocationOptions.SectionName)
            .Get<LocationOptions>() ?? new LocationOptions();

        Assert.Equal("GB", options.DefaultCountryCode);
        Assert.Equal("London", options.DefaultCityName);
    }
}
