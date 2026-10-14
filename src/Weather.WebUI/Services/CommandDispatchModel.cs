namespace Weather.WebUI.Services;

/// <summary>
/// Testable state for the command dispatch form. Favorite posts to the favorites endpoint,
/// Note posts to the notes endpoint, so the intent mapping is verified offline.
/// </summary>
public sealed class CommandDispatchModel(WeatherApiClient api)
{
    public MutationIntent Intent { get; set; } = MutationIntent.Favorite;

    public string Text { get; set; } = string.Empty;

    public CityModel? City { get; private set; }

    public decimal? TemperatureCelsius { get; private set; }

    public bool Posting { get; private set; }

    public string? ValidationError { get; private set; }

    public ApiProblem? Error { get; private set; }

    public CommandOutcome? LastResult { get; private set; }

    public bool CanSubmit => City is not null && !string.IsNullOrWhiteSpace(Text) && !Posting;

    public void Bind(CityModel? city, decimal? temperatureCelsius)
    {
        City = city;
        TemperatureCelsius = temperatureCelsius;
    }

    public async Task SubmitAsync(CancellationToken cancellationToken)
    {
        ValidationError = null;
        Error = null;

        if (City is null)
        {
            ValidationError = "Select a city first.";
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
                    new AddFavoriteModel(City.Name, City.CountryCode), cancellationToken);
                LastResult = new CommandOutcome(result.Status, result.IsSuccess, result.RawJson);
            }
            else
            {
                ApiResult<WeatherNoteModel> result = await api.AddNoteAsync(
                    new AddNoteModel(City.Name, City.CountryCode, Text, TemperatureCelsius), cancellationToken);
                LastResult = new CommandOutcome(result.Status, result.IsSuccess, result.RawJson);
            }
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
