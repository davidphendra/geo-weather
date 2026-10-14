using NSubstitute;

using Weather.Modules.Geography.Application.Abstractions;
using Weather.Modules.Geography.Application.Cities.GetCitiesByCountry;
using Weather.Modules.Geography.Domain;
using Weather.Modules.Geography.Domain.Cities;
using Weather.Modules.Geography.Domain.Countries;

namespace Weather.Modules.Geography.Tests.UnitTests.Application;

[Trait("Category", "GetCities")]
public sealed class GetCitiesByCountryQueryHandlerTests
{
    private static City MakeCity(string name) => City.Create(
        CityId.New(),
        CountryCode.From("GB"),
        name,
        GeoCoordinate.From(51.5074m, -0.1278m));

    [Fact]
    public async Task ReturnsCities_WhenCountryExists()
    {
        ICountryReadRepository countries = Substitute.For<ICountryReadRepository>();
        countries.ExistsAsync(Arg.Any<CountryCode>(), Arg.Any<CancellationToken>()).Returns(true);
        ICityReadRepository cities = Substitute.For<ICityReadRepository>();
        cities.GetByCountryAsync(Arg.Any<CountryCode>(), Arg.Any<CancellationToken>()).Returns([MakeCity("London")]);
        var handler = new GetCitiesByCountryQueryHandler(countries, cities);

        IReadOnlyList<CityDto> result = await handler.Handle(new GetCitiesByCountryQuery("GB"), CancellationToken.None);

        Assert.Single(result);
        Assert.Equal("London", result[0].Name);
    }

    [Fact]
    public async Task Throws_WhenCountryUnknown()
    {
        ICountryReadRepository countries = Substitute.For<ICountryReadRepository>();
        countries.ExistsAsync(Arg.Any<CountryCode>(), Arg.Any<CancellationToken>()).Returns(false);
        ICityReadRepository cities = Substitute.For<ICityReadRepository>();
        var handler = new GetCitiesByCountryQueryHandler(countries, cities);

        await Assert.ThrowsAsync<CountryNotFoundException>(
            () => handler.Handle(new GetCitiesByCountryQuery("ZZ"), CancellationToken.None));
    }
}
