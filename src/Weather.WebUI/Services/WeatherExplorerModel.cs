namespace Weather.WebUI.Services;

/// <summary>
/// Testable state for the Weather Explorer cascade (country → cities → current weather),
/// including loading and Problem-Details error state. Kept out of the component so the
/// behaviour can be verified offline without a browser.
/// </summary>
public sealed class WeatherExplorerModel(WeatherApiClient api)
{
    public IReadOnlyList<CountryModel> Countries { get; private set; } = [];

    public IReadOnlyList<CityModel> Cities { get; private set; } = [];

    public CountryModel? Country { get; private set; }

    public CityModel? City { get; private set; }

    public WeatherReportModel? Report { get; private set; }

    public bool Loading { get; private set; }

    public ApiProblem? Error { get; private set; }

    public async Task InitializeAsync(CancellationToken cancellationToken)
    {
        Loading = true;
        Error = null;

        try
        {
            Countries = await api.GetCountriesAsync(cancellationToken);
            if (Countries.Count > 0)
            {
                await LoadCitiesAsync(Countries[0], cancellationToken);
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

        Cities = await api.GetCitiesAsync(country.Code, cancellationToken);
        if (Cities.Count > 0)
        {
            await LoadReportAsync(Cities[0], cancellationToken);
        }
    }

    private async Task LoadReportAsync(CityModel city, CancellationToken cancellationToken)
    {
        City = city;
        Report = null;
        Report = await api.GetWeatherAsync(city.Name, city.CountryCode, cancellationToken);
    }
}
