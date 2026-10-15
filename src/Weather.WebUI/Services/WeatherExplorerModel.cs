using Microsoft.Extensions.Options;

using Weather.WebUI.Configuration;

namespace Weather.WebUI.Services;

/// <summary>
/// Testable state for the Weather Explorer cascade (country → cities → current weather),
/// including loading and Problem-Details error state. Kept out of the component so the
/// behaviour can be verified offline without a browser.
/// </summary>
public sealed class WeatherExplorerModel(WeatherApiClient api, IOptionsSnapshot<LocationOptions> options)
{
    public IReadOnlyList<CountryModel> Countries { get; private set; } = [];

    public IReadOnlyList<CityModel> Cities { get; private set; } = [];

    public CountryModel? Country { get; private set; }

    public CityModel? City { get; private set; }

    public WeatherReportModel? Report { get; private set; }

    public WeatherForecastModel? Forecast { get; private set; }

    public bool Loading { get; private set; }

    public ApiProblem? Error { get; private set; }

    public async Task InitializeAsync(CancellationToken cancellationToken)
    {
        Loading = true;
        Error = null;

        try
        {
            Countries = await api.GetCountriesAsync(cancellationToken);
            CountryModel? country = ResolveDefaultCountry();
            if (country is not null)
            {
                await LoadCitiesAsync(country, cancellationToken);
            }
        }
        catch (WeatherApiException exception)
        {
            Error = exception.Problem;
        }
        finally
        {
            Loading = false;
        }
    }

    public async Task SelectCountryAsync(CountryModel country, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(country);

        Loading = true;
        Error = null;

        try
        {
            await LoadCitiesAsync(country, cancellationToken);
        }
        catch (WeatherApiException exception)
        {
            Error = exception.Problem;
        }
        finally
        {
            Loading = false;
        }
    }

    public async Task SelectCityAsync(CityModel city, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(city);

        Loading = true;
        Error = null;

        try
        {
            await LoadReportAsync(city, cancellationToken);
        }
        catch (WeatherApiException exception)
        {
            Error = exception.Problem;
        }
        finally
        {
            Loading = false;
        }
    }

    private async Task LoadCitiesAsync(CountryModel country, CancellationToken cancellationToken)
    {
        Country = country;
        City = null;
        Report = null;
        Forecast = null;

        Cities = await api.GetCitiesAsync(country.Code, cancellationToken);
        CityModel? city = ResolveDefaultCity(country);
        if (city is not null)
        {
            await LoadReportAsync(city, cancellationToken);
        }
    }

    private async Task LoadReportAsync(CityModel city, CancellationToken cancellationToken)
    {
        City = city;
        Report = null;
        Forecast = null;

        Report = await api.GetWeatherAsync(city.Name, city.CountryCode, cancellationToken);

        try
        {
            Forecast = await api.GetForecastAsync(city.Name, city.CountryCode, cancellationToken);
        }
        catch (WeatherApiException)
        {
            // ADR-0017 D5: the forecast is additive - a forecast failure must not blank current conditions.
            Forecast = null;
        }
    }

    /// <summary>The configured country, or the first country when it is blank or unknown.</summary>
    private CountryModel? ResolveDefaultCountry()
    {
        string code = options.Value.DefaultCountryCode;
        if (!string.IsNullOrWhiteSpace(code))
        {
            CountryModel? match = Countries.FirstOrDefault(
                country => string.Equals(country.Code, code, StringComparison.OrdinalIgnoreCase));
            if (match is not null)
            {
                return match;
            }
        }

        return Countries.Count > 0 ? Countries[0] : null;
    }

    /// <summary>
    /// The configured city when the default country is being loaded, otherwise the first city.
    /// The configured name is ignored for other countries so manual selection keeps first-city behaviour.
    /// </summary>
    private CityModel? ResolveDefaultCity(CountryModel country)
    {
        string defaultCountryCode = options.Value.DefaultCountryCode;
        string defaultCityName = options.Value.DefaultCityName;

        if (!string.IsNullOrWhiteSpace(defaultCityName)
            && string.Equals(country.Code, defaultCountryCode, StringComparison.OrdinalIgnoreCase))
        {
            CityModel? match = Cities.FirstOrDefault(
                city => string.Equals(city.Name, defaultCityName, StringComparison.OrdinalIgnoreCase));
            if (match is not null)
            {
                return match;
            }
        }

        return Cities.Count > 0 ? Cities[0] : null;
    }
}
