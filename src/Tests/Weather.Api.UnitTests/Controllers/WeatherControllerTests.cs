using Microsoft.AspNetCore.Mvc;

using NSubstitute;

using Weather.Api.Modules.Weather;
using Weather.Modules.Weather.Application.Contracts;
using Weather.Modules.Weather.Application.WeatherReports.GetCurrentWeather;

namespace Weather.Api.UnitTests.Controllers;

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

        ActionResult<WeatherReportDto> result = await controller.GetCurrentWeather("London", "GB", CancellationToken.None);

        OkObjectResult ok = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Equal(Report(), ok.Value);
    }

    [Fact]
    public async Task GetCurrentWeather_ReturnsNotFoundWhenMissing()
    {
        IWeatherService service = Substitute.For<IWeatherService>();
        service.GetCurrentWeatherAsync("Nowhere", "GB", Arg.Any<CancellationToken>()).Returns((WeatherReportDto?)null);
        var controller = new WeatherController(service);

        ActionResult<WeatherReportDto> result = await controller.GetCurrentWeather("Nowhere", "GB", CancellationToken.None);

        Assert.IsType<NotFoundResult>(result.Result);
    }
}
