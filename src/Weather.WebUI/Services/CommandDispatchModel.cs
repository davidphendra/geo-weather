namespace Weather.WebUI.Services;

/// <summary>
/// Testable state for a command dispatch form. Favorite posts to the favorites endpoint, Note posts
/// to the notes endpoint, so the intent mapping is verified offline. The target can be a bound city
/// (Explorer) or free text (Favorites quick-add).
/// </summary>
public sealed class CommandDispatchModel(WeatherApiClient api)
{
    private readonly List<CommandLogEntry> _history = [];

    public MutationIntent Intent { get; set; } = MutationIntent.Favorite;

    public string Text { get; set; } = string.Empty;

    public string? TargetCityName { get; private set; }

    public string? TargetCountryCode { get; private set; }

    public decimal? TemperatureCelsius { get; private set; }

    public bool Posting { get; private set; }

    public string? ValidationError { get; private set; }

    public ApiProblem? Error { get; private set; }

    public CommandOutcome? LastResult { get; private set; }

    /// <summary>Most recent first: every command this circuit has dispatched.</summary>
    public IReadOnlyList<CommandLogEntry> History => _history;

    public bool CanSubmit =>
        !string.IsNullOrWhiteSpace(TargetCityName)
        && !string.IsNullOrWhiteSpace(TargetCountryCode)
        && !string.IsNullOrWhiteSpace(Text)
        && !Posting;

    public void Bind(CityModel? city, decimal? temperatureCelsius)
    {
        TargetCityName = city?.Name;
        TargetCountryCode = city?.CountryCode;
        TemperatureCelsius = temperatureCelsius;
    }

    public void BindManual(string? cityName, string? countryCode, decimal? temperatureCelsius = null)
    {
        TargetCityName = cityName;
        TargetCountryCode = countryCode;
        TemperatureCelsius = temperatureCelsius;
    }

    public void ClearHistory() => _history.Clear();

    public async Task SubmitAsync(CancellationToken cancellationToken)
    {
        ValidationError = null;
        Error = null;

        if (string.IsNullOrWhiteSpace(TargetCityName) || string.IsNullOrWhiteSpace(TargetCountryCode))
        {
            ValidationError = "Select or enter a target city and country.";
            return;
        }

        if (string.IsNullOrWhiteSpace(Text))
        {
            ValidationError = "Enter a label or note.";
            return;
        }

        Posting = true;

        try
        {
            if (Intent == MutationIntent.Favorite)
            {
                ApiResult<FavoriteCityModel> result = await api.AddFavoriteAsync(
                    new AddFavoriteModel(TargetCityName, TargetCountryCode), cancellationToken);
                LastResult = new CommandOutcome(result.Status, result.IsSuccess, result.RawJson);
            }
            else
            {
                ApiResult<WeatherNoteModel> result = await api.AddNoteAsync(
                    new AddNoteModel(TargetCityName, TargetCountryCode, Text, TemperatureCelsius), cancellationToken);
                LastResult = new CommandOutcome(result.Status, result.IsSuccess, result.RawJson);
            }

            _history.Insert(0, new CommandLogEntry(DateTimeOffset.UtcNow, Intent, LastResult));
        }
        catch (WeatherApiException exception)
        {
            Error = exception.Problem;
        }
        finally
        {
            Posting = false;
        }
    }
}
