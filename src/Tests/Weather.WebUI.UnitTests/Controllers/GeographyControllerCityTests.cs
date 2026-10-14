using Microsoft.AspNetCore.Mvc;

using NSubstitute;

using Weather.Modules.Geography.Application.Cities.GetCitiesByCountry;
using Weather.Modules.Geography.Application.Contracts;
using Weather.WebUI.Api.Modules.Geography;

namespace Weather.WebUI.UnitTests.Controllers;

[Trait("Category", "GeographyController")]
public sealed class GeographyControllerCityTests
{
    [Fact]
    public async Task GetCitiesByCountry_ReturnsOkWithCities()
    {
        IGeographyService service = Substitute.For<IGeographyService>();
        service.GetCitiesAsync("GB", Arg.Any<CancellationToken>())
            .Returns([new CityDto("00000001-0000-0000-0000-000000000000", "London", "GB", 51.5074m, -0.1278m)]);
        var controller = new GeographyController(service);

        ActionResult<IReadOnlyList<CityDto>> result = await controller.GetCitiesByCountryAsync("GB", CancellationToken.None);

        OkObjectResult ok = Assert.IsType<OkObjectResult>(result.Result);
        IReadOnlyList<CityDto> cities = Assert.IsAssignableFrom<IReadOnlyList<CityDto>>(ok.Value);
        Assert.Single(cities);
        Assert.Equal("GB", cities[0].CountryCode);
    }
}
