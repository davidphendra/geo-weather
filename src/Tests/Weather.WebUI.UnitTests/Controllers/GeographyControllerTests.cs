using Microsoft.AspNetCore.Mvc;

using NSubstitute;

using Weather.Modules.Geography.Application.Contracts;
using Weather.Modules.Geography.Application.Countries.GetCountries;
using Weather.WebUI.Api.Modules.Geography;

namespace Weather.WebUI.UnitTests.Controllers;

[Trait("Category", "GeographyController")]
public sealed class GeographyControllerTests
{
    [Fact]
    public async Task GetCountries_ReturnsOkWithCountries()
    {
        IGeographyService service = Substitute.For<IGeographyService>();
        service.GetCountriesAsync(null, Arg.Any<CancellationToken>())
            .Returns([new CountryDto("GB", "United Kingdom")]);
        var controller = new GeographyController(service);

        ActionResult<IReadOnlyList<CountryDto>> result = await controller.GetCountries(null, CancellationToken.None);

        OkObjectResult ok = Assert.IsType<OkObjectResult>(result.Result);
        IReadOnlyList<CountryDto> countries = Assert.IsAssignableFrom<IReadOnlyList<CountryDto>>(ok.Value);
        Assert.Single(countries);
        Assert.Equal("GB", countries[0].Code);
    }

    [Fact]
    public async Task GetCountries_PassesTheSearchTermThrough()
    {
        IGeographyService service = Substitute.For<IGeographyService>();
        service.GetCountriesAsync(Arg.Any<string?>(), Arg.Any<CancellationToken>()).Returns([]);
        var controller = new GeographyController(service);

        await controller.GetCountries("united", CancellationToken.None);

        await service.Received(1).GetCountriesAsync("united", Arg.Any<CancellationToken>());
    }
}
