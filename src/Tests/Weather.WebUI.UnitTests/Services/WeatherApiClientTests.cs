using System.Net;
using System.Text;

using Weather.WebUI.Services;
using Weather.WebUI.UnitTests.Support;

namespace Weather.WebUI.UnitTests.Services;

/// <summary>Typed client behaviour against the same-origin API (e06s02).</summary>
[Trait("Category", "WeatherApiClient")]
public sealed class WeatherApiClientTests
{
    private const string WeatherJson = """
        {
          "location": { "city": "London", "country": "GB" },
          "observedAtUtc": "2025-01-01T12:00:00+00:00",
          "temperature": { "celsius": 10.0, "fahrenheit": 50.0 },
          "dewPoint": { "celsius": 8.0, "fahrenheit": 46.4 },
          "relativeHumidityPercent": 80,
          "wind": { "speedMetersPerSecond": 4.0, "directionDegrees": 220, "cardinal": "SW" },
          "visibilityMeters": 10000,
          "pressureHectopascals": 1013,
          "skyCondition": "Cloudy"
        }
        """;

    private static WeatherApiClient Create(Func<HttpRequestMessage, HttpResponseMessage> respond) =>
        new(new HttpClient(new StubHttpMessageHandler(respond)), new TestNavigationManager());

    private static HttpResponseMessage Json(string json, HttpStatusCode status = HttpStatusCode.OK) =>
        new(status) { Content = new StringContent(json, Encoding.UTF8, "application/json") };

    [Fact]
    public async Task GetCountries_parses_the_catalog()
    {
        WeatherApiClient client = Create(_ => Json("""[{"code":"GB","name":"United Kingdom"}]"""));

        IReadOnlyList<CountryModel> countries = await client.GetCountriesAsync(CancellationToken.None);

        CountryModel country = Assert.Single(countries);
        Assert.Equal("GB", country.Code);
        Assert.Equal("United Kingdom", country.Name);
    }

    [Fact]
    public async Task GetWeather_parses_every_metric()
    {
        WeatherApiClient client = Create(_ => Json(WeatherJson));

        WeatherReportModel report = await client.GetWeatherAsync("London", "GB", CancellationToken.None);

        Assert.Equal("London", report.Location.City);
        Assert.Equal(10.0m, report.Temperature.Celsius);
        Assert.Equal(8.0m, report.DewPoint.Celsius);
        Assert.Equal(80, report.RelativeHumidityPercent);
        Assert.Equal("SW", report.Wind.Cardinal);
        Assert.Equal(1013m, report.PressureHectopascals);
        Assert.Equal("Cloudy", report.SkyCondition);
    }

    [Fact]
    public async Task Requests_are_absolute_on_the_app_origin()
    {
        Uri? requested = null;
        WeatherApiClient client = Create(request =>
        {
            requested = request.RequestUri;
            return Json(WeatherJson);
        });

        await client.GetWeatherAsync("London", "GB", CancellationToken.None);

        Assert.Equal("https://test.local/api/weather/London?countryCode=GB", requested?.ToString());
    }

    [Fact]
    public async Task A_404_is_mapped_to_a_weather_api_exception()
    {
        WeatherApiClient client = Create(_ => Json(
            """{"title":"Not Found","status":404,"detail":"Unknown city.","code":"RESOURCE_NOT_FOUND"}""",
            HttpStatusCode.NotFound));

        WeatherApiException exception = await Assert.ThrowsAsync<WeatherApiException>(
            () => client.GetWeatherAsync("Nowhere", "GB", CancellationToken.None));

        Assert.Equal(404, exception.Problem.Status);
        Assert.Equal("RESOURCE_NOT_FOUND", exception.Problem.Code);
    }

    [Fact]
    public async Task A_502_is_mapped_to_a_weather_api_exception()
    {
        WeatherApiClient client = Create(_ => Json(
            """{"title":"Bad Gateway","status":502,"detail":"Upstream down.","code":"DEPENDENCY_UNAVAILABLE"}""",
            HttpStatusCode.BadGateway));

        WeatherApiException exception = await Assert.ThrowsAsync<WeatherApiException>(
            () => client.GetWeatherAsync("London", "GB", CancellationToken.None));

        Assert.Equal("DEPENDENCY_UNAVAILABLE", exception.Problem.Code);
    }

    [Fact]
    public async Task AddFavorite_returns_the_created_resource_and_status()
    {
        WeatherApiClient client = Create(_ => Json(
            """
            {
              "id": "11111111-1111-1111-1111-111111111111",
              "cityId": "22222222-2222-2222-2222-222222222222",
              "city": "London",
              "countryCode": "GB",
              "createdAtUtc": "2025-01-01T00:00:00+00:00"
            }
            """,
            HttpStatusCode.Created));

        ApiResult<FavoriteCityModel> result = await client.AddFavoriteAsync(
            new AddFavoriteModel("London", "GB"), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(201, result.Status);
        Assert.Equal("London", result.Value?.City);
    }
}
