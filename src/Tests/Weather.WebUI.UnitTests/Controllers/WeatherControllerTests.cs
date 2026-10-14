using Microsoft.AspNetCore.Mvc;

using NSubstitute;

using Weather.Modules.Weather.Application.Contracts;
using Weather.Modules.Weather.Application.WeatherReports.GetCurrentWeather;
using Weather.WebUI.Api.Modules.Weather;

namespace Weather.WebUI.UnitTests.Controllers;

[Trait("Category", "WeatherController")]
public sealed class WeatherControllerTests
{
    private static WeatherReportDto Report() => new(
        new LocationDto("London", "GB"),
        DateTimeOffset.UnixEpoch,
        new TemperatureDto(10m, 50m),
        new TemperatureDto(8m, 46.4m),
        80,
        new WindDto(4m, 220, "SW"),
        10000m,
        1013m,
        "Cloudy");

    [Fact]
    public async Task GetCurrentWeather_ReturnsOkWhenFound()
    {
        IWeatherService service = Substitute.For<IWeatherService>();
        service.GetCurrentWeatherAsync("London", "GB", Arg.Any<CancellationToken>()).Returns(Report());
        var controller = new WeatherController(service);

        ActionResult<WeatherReportDto> result = await controller.GetCurrentWeatherAsync("London", "GB", CancellationToken.None);

        OkObjectResult ok = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Equal(Report(), ok.Value);
    }

}
