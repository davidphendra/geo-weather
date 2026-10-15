using System.Net;
using System.Text;

using Microsoft.Extensions.Options;

using NSubstitute;

using Weather.WebUI.Configuration;
using Weather.WebUI.Services;
using Weather.WebUI.UnitTests.Support;

namespace Weather.WebUI.UnitTests.Services;

/// <summary>The country → city → weather cascade and its error state (e06s02).</summary>
[Trait("Category", "WeatherExplorerModel")]
public sealed class WeatherExplorerModelTests
{
    private const string WeatherJson = """
        {
          "location": { "city": "London", "country": "GB" },
          "latitude": 51.5074,
          "longitude": -0.1278,
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

    private static WeatherApiClient Client(Func<HttpRequestMessage, HttpResponseMessage> respond) =>
        new(new HttpClient(new StubHttpMessageHandler(respond)), new TestNavigationManager());

    private static WeatherExplorerModel Model(
        Func<HttpRequestMessage, HttpResponseMessage> respond,
        LocationOptions? options = null)
    {
        IOptionsSnapshot<LocationOptions> snapshot = Substitute.For<IOptionsSnapshot<LocationOptions>>();
        snapshot.Value.Returns(options ?? new LocationOptions
        {
            DefaultCountryCode = "ID",
            DefaultCityName = "Jakarta",
        });

        return new WeatherExplorerModel(Client(respond), snapshot);
    }

    private static HttpResponseMessage Json(string json, HttpStatusCode status = HttpStatusCode.OK) =>
        new(status) { Content = new StringContent(json, Encoding.UTF8, "application/json") };

    private static HttpResponseMessage HappyPath(HttpRequestMessage request) =>
        request.RequestUri?.AbsolutePath switch
        {
            "/api/countries" => Json("""[{"code":"GB","name":"United Kingdom"}]"""),
            "/api/countries/GB/cities" => Json(
                """[{"id":"c1","name":"London","countryCode":"GB","latitude":51.5,"longitude":-0.1}]"""),
            "/api/weather/London" => Json(WeatherJson),
            "/api/weather/London/forecast" => Json(ForecastJson),
            _ => Json("""{"status":404,"code":"RESOURCE_NOT_FOUND"}""", HttpStatusCode.NotFound),
        };

    private static HttpResponseMessage IndonesiaPath(HttpRequestMessage request) =>
        request.RequestUri?.AbsolutePath switch
        {
            "/api/countries" => Json(
                """[{"code":"ID","name":"Indonesia"},{"code":"GB","name":"United Kingdom"}]"""),
            "/api/countries/ID/cities" => Json(
                """
                [{"id":"id1","name":"Jakarta","countryCode":"ID","latitude":-6.2,"longitude":106.8},
                 {"id":"id2","name":"Bandung","countryCode":"ID","latitude":-6.9,"longitude":107.6}]
                """),
            "/api/countries/GB/cities" => Json(
                """[{"id":"gb1","name":"London","countryCode":"GB","latitude":51.5,"longitude":-0.1}]"""),
            "/api/weather/Jakarta" => Json(WeatherJson),
            "/api/weather/Jakarta/forecast" => Json(ForecastJson),
            "/api/weather/Bandung" => Json(WeatherJson),
            "/api/weather/Bandung/forecast" => Json(ForecastJson),
            "/api/weather/London" => Json(WeatherJson),
            "/api/weather/London/forecast" => Json(ForecastJson),
            _ => Json("""{"status":404,"code":"RESOURCE_NOT_FOUND"}""", HttpStatusCode.NotFound),
        };

    [Fact]
    public async Task Initialize_loads_countries_cities_and_the_report()
    {
        var model = Model(HappyPath);

        await model.InitializeAsync(CancellationToken.None);

        Assert.Equal("United Kingdom", model.Country?.Name);
        Assert.Equal("London", model.City?.Name);
        Assert.NotNull(model.Report);
        Assert.Equal(51.5074m, model.Report!.Latitude);
        Assert.Null(model.Error);
        Assert.False(model.Loading);
    }

    [Fact]
    public async Task Selecting_a_country_loads_its_cities_and_report()
    {
        var model = Model(HappyPath);

        await model.SelectCountryAsync(new CountryModel("GB", "United Kingdom"), CancellationToken.None);

        CityModel city = Assert.Single(model.Cities);
        Assert.Equal("London", city.Name);
        Assert.NotNull(model.Report);
    }

    [Fact]
    public async Task A_weather_failure_surfaces_as_an_error_state()
    {
        var model = Model(request =>
            request.RequestUri?.AbsolutePath == "/api/weather/London"
                ? Json(
                    """{"title":"Bad Gateway","status":502,"detail":"Upstream down.","code":"DEPENDENCY_UNAVAILABLE"}""",
                    HttpStatusCode.BadGateway)
                : HappyPath(request));

        await model.InitializeAsync(CancellationToken.None);

        Assert.Null(model.Report);
        Assert.Equal("DEPENDENCY_UNAVAILABLE", model.Error?.Code);
        Assert.False(model.Loading);
    }

    [Fact]
    public async Task Initialize_loads_the_forecast()
    {
        var model = Model(HappyPath);

        await model.InitializeAsync(CancellationToken.None);

        WeatherForecastModel forecast = Assert.IsType<WeatherForecastModel>(model.Forecast);
        Assert.Single(forecast.Days);
    }

    [Fact]
    public async Task A_forecast_failure_keeps_current_conditions()
    {
        var model = Model(request =>
            request.RequestUri?.AbsolutePath == "/api/weather/London/forecast"
                ? Json(
                    """{"title":"Bad Gateway","status":502,"detail":"Upstream down.","code":"DEPENDENCY_UNAVAILABLE"}""",
                    HttpStatusCode.BadGateway)
                : HappyPath(request));

        await model.InitializeAsync(CancellationToken.None);

        Assert.NotNull(model.Report);
        Assert.Null(model.Forecast);
        Assert.Null(model.Error);
    }

    [Fact]
    public async Task Initialize_selects_the_configured_default_country_and_city()
    {
        var model = Model(IndonesiaPath, new LocationOptions
        {
            DefaultCountryCode = "ID",
            DefaultCityName = "Jakarta",
        });

        await model.InitializeAsync(CancellationToken.None);

        Assert.Equal("ID", model.Country?.Code);
        Assert.Equal("Jakarta", model.City?.Name);
        Assert.NotNull(model.Report);
    }

    [Fact]
    public async Task Initialize_falls_back_to_the_first_country_and_city_when_the_defaults_are_unknown()
    {
        var model = Model(IndonesiaPath, new LocationOptions
        {
            DefaultCountryCode = "ZZ",
            DefaultCityName = "Nowhere",
        });

        await model.InitializeAsync(CancellationToken.None);

        Assert.Equal("ID", model.Country?.Code);
        Assert.Equal("Jakarta", model.City?.Name);
    }

    [Fact]
    public async Task Initialize_honours_a_different_configured_default()
    {
        var model = Model(IndonesiaPath, new LocationOptions
        {
            DefaultCountryCode = "GB",
            DefaultCityName = "London",
        });

        await model.InitializeAsync(CancellationToken.None);

        Assert.Equal("GB", model.Country?.Code);
        Assert.Equal("London", model.City?.Name);
    }

    [Fact]
    public async Task Initialize_falls_back_to_the_first_city_when_the_configured_city_is_absent()
    {
        var model = Model(IndonesiaPath, new LocationOptions
        {
            DefaultCountryCode = "ID",
            DefaultCityName = "Surabaya",
        });

        await model.InitializeAsync(CancellationToken.None);

        Assert.Equal("ID", model.Country?.Code);
        Assert.Equal("Jakarta", model.City?.Name);
    }
}
