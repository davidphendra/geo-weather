using System.Text.Json;

using Microsoft.AspNetCore.Components;

namespace Weather.WebUI.Services;

/// <summary>
/// Typed client for the same-origin REST API (ADR-0012). Components render server-side
/// (<c>InteractiveServer</c>), so request URIs are made absolute against the app's own origin
/// taken from <see cref="NavigationManager.BaseUri"/>; there is deliberately no configurable
/// base address (D7).
/// </summary>
public sealed class WeatherApiClient(HttpClient http, NavigationManager navigation)
{
    public Task<IReadOnlyList<CountryModel>> GetCountriesAsync(CancellationToken cancellationToken) =>
        GetAsync<IReadOnlyList<CountryModel>>("api/countries", cancellationToken);

    public Task<IReadOnlyList<CityModel>> GetCitiesAsync(string countryCode, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(countryCode);

        return GetAsync<IReadOnlyList<CityModel>>(
            $"api/countries/{Uri.EscapeDataString(countryCode)}/cities", cancellationToken);
    }

    public Task<WeatherReportModel> GetWeatherAsync(
        string cityName,
        string countryCode,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(cityName);
        ArgumentException.ThrowIfNullOrWhiteSpace(countryCode);

        return GetAsync<WeatherReportModel>(
            $"api/weather/{Uri.EscapeDataString(cityName)}?countryCode={Uri.EscapeDataString(countryCode)}",
            cancellationToken);
    }

    public Task<WeatherForecastModel> GetForecastAsync(
        string cityName,
        string countryCode,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(cityName);
        ArgumentException.ThrowIfNullOrWhiteSpace(countryCode);

        return GetAsync<WeatherForecastModel>(
            $"api/weather/{Uri.EscapeDataString(cityName)}/forecast?countryCode={Uri.EscapeDataString(countryCode)}",
            cancellationToken);
    }

    public Task<IReadOnlyList<FavoriteCityModel>> GetFavoritesAsync(CancellationToken cancellationToken) =>
        GetAsync<IReadOnlyList<FavoriteCityModel>>("api/cities/favorites", cancellationToken);

    public Task<ApiResult<FavoriteCityModel>> AddFavoriteAsync(
        AddFavoriteModel body,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(body);

        return PostAsync<FavoriteCityModel>("api/cities/favorites", body, cancellationToken);
    }

    public Task<ApiResult<WeatherNoteModel>> AddNoteAsync(AddNoteModel body, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(body);

        return PostAsync<WeatherNoteModel>("api/weather/notes", body, cancellationToken);
    }

    private Uri Endpoint(string relative) => new(new Uri(navigation.BaseUri, UriKind.Absolute), relative);

    private static ApiProblem DependencyUnavailable() => new(
        null,
        "Dependency unavailable",
        StatusCodes.Status502BadGateway,
        "The weather API could not be reached.",
        "DEPENDENCY_UNAVAILABLE");

    private static async Task<HttpResponseMessage> SendAsync(Func<Task<HttpResponseMessage>> send)
    {
        try
        {
            return await send();
        }
        catch (HttpRequestException exception)
        {
            throw new WeatherApiException(DependencyUnavailable(), exception);
        }
    }

    private async Task<T> GetAsync<T>(string relative, CancellationToken cancellationToken)
    {
        using HttpResponseMessage response = await SendAsync(() => http.GetAsync(Endpoint(relative), cancellationToken));

        if (!response.IsSuccessStatusCode)
        {
            throw new WeatherApiException(await ReadProblemAsync(response, cancellationToken));
        }

        return await response.Content.ReadFromJsonAsync<T>(cancellationToken)
            ?? throw new WeatherApiException(
                new ApiProblem(null, "Empty response", (int)response.StatusCode, null, null));
    }

    private async Task<ApiResult<T>> PostAsync<T>(string relative, object body, CancellationToken cancellationToken)
    {
        using HttpResponseMessage response = await SendAsync(
            () => http.PostAsJsonAsync(Endpoint(relative), body, cancellationToken));
        string json = await response.Content.ReadAsStringAsync(cancellationToken);

        T? value = response.IsSuccessStatusCode
            ? JsonSerializer.Deserialize<T>(json, JsonSerializerOptions.Web)
            : default;

        return new ApiResult<T>((int)response.StatusCode, response.IsSuccessStatusCode, value, json);
    }

    private static async Task<ApiProblem> ReadProblemAsync(
        HttpResponseMessage response,
        CancellationToken cancellationToken)
    {
        var body = await response.Content.ReadAsStringAsync(cancellationToken);

        try
        {
            using JsonDocument document = JsonDocument.Parse(body);
            JsonElement root = document.RootElement;

            return new ApiProblem(
                ReadString(root, "type"),
                ReadString(root, "title"),
                (int)response.StatusCode,
                ReadString(root, "detail"),
                ReadString(root, "code"));
        }
        catch (JsonException)
        {
            return new ApiProblem(null, response.ReasonPhrase, (int)response.StatusCode, null, null);
        }
    }

    private static string? ReadString(JsonElement element, string property) =>
        element.TryGetProperty(property, out JsonElement value) && value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : null;
}
