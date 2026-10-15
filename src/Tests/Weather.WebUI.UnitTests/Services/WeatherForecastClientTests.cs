using System.Net;

using Weather.WebUI.Services;
using Weather.WebUI.UnitTests.Support;

namespace Weather.WebUI.UnitTests.Services;

/// <summary>Five-day forecast client behaviour against the same-origin API (e14s04).</summary>
[Trait("Category", "WeatherApiClient")]
public sealed class WeatherForecastClientTests
{
    private const string ForecastJson = """
        {
          "location": { "city": "London", "country": "GB" },
          "days": [
            {
              "date": "2025-01-01",
              "high": { "celsius": 14.0, "fahrenheit": 57.2 },
              "low": { "celsius": 8.0, "fahrenheit": 46.4 },
              "precipitationProbabilityPercent": 75,
              "skyCondition": "Rain"
            }
          ]
        }
        """;

    private static WeatherApiClient Create(Func<HttpRequestMessage, HttpResponseMessage> respond) =>
        new(new HttpClient(new StubHttpMessageHandler(respond)), new TestNavigationManager());

    [Fact]
    public async Task GetForecast_parses_the_days()
    {
        WeatherApiClient client = Create(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(ForecastJson, System.Text.Encoding.UTF8, "application/json"),
        });

        WeatherForecastModel forecast = await client.GetForecastAsync("London", "GB", CancellationToken.None);

        Assert.Equal("London", forecast.Location.City);

        ForecastDayModel day = Assert.Single(forecast.Days);
        Assert.Equal(new DateOnly(2025, 1, 1), day.Date);
        Assert.Equal(14.0m, day.High.Celsius);
        Assert.Equal(8.0m, day.Low.Celsius);
        Assert.Equal(75, day.PrecipitationProbabilityPercent);
        Assert.Equal("Rain", day.SkyCondition);
    }

    [Fact]
    public async Task Requests_the_forecast_route_on_the_app_origin()
    {
        Uri? requested = null;
        WeatherApiClient client = Create(request =>
        {
            requested = request.RequestUri;
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(ForecastJson, System.Text.Encoding.UTF8, "application/json"),
            };
        });

        await client.GetForecastAsync("London", "GB", CancellationToken.None);

        Assert.Equal("https://test.local/api/weather/London/forecast?countryCode=GB", requested?.ToString());
    }

    [Fact]
    public async Task A_502_is_mapped_to_a_weather_api_exception()
    {
        WeatherApiClient client = Create(_ => new HttpResponseMessage(HttpStatusCode.BadGateway)
        {
            Content = new StringContent(
                """{"title":"Bad Gateway","status":502,"detail":"Upstream down.","code":"DEPENDENCY_UNAVAILABLE"}""",
                System.Text.Encoding.UTF8,
                "application/json"),
        });

        WeatherApiException exception = await Assert.ThrowsAsync<WeatherApiException>(
            () => client.GetForecastAsync("London", "GB", CancellationToken.None));

        Assert.Equal("DEPENDENCY_UNAVAILABLE", exception.Problem.Code);
    }
}
