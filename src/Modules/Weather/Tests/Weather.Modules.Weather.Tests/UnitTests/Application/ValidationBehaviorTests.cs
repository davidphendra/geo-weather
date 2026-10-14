using FluentValidation;

using global::Weather.BuildingBlocks.Application.Configuration;
using global::Weather.Modules.Weather.Application.WeatherReports.GetCurrentWeather;

using MediatR;

namespace Weather.Modules.Weather.Tests.UnitTests.Application;

[Trait("Category", "ValidationBehavior")]
public sealed class ValidationBehaviorTests
{
    private static readonly ValidationBehavior<GetCurrentWeatherQuery, WeatherReportDto?> Behavior =
        new([new GetCurrentWeatherQueryValidator()]);

    [Fact]
    public async Task ThrowsWhenValidationFails_AndDoesNotCallNext()
    {
        var nextCalled = false;
        RequestHandlerDelegate<WeatherReportDto?> next = _ =>
        {
            nextCalled = true;
            return Task.FromResult<WeatherReportDto?>(null);
        };

        await Assert.ThrowsAsync<ValidationException>(() =>
            Behavior.Handle(new GetCurrentWeatherQuery(string.Empty, "GB"), next, CancellationToken.None));

        Assert.False(nextCalled);
    }

    [Fact]
    public async Task CallsNextWhenValid()
    {
        RequestHandlerDelegate<WeatherReportDto?> next = _ => Task.FromResult<WeatherReportDto?>(null);

        WeatherReportDto? result = await Behavior.Handle(
            new GetCurrentWeatherQuery("London", "GB"),
            next,
            CancellationToken.None);

        Assert.Null(result);
    }
}
