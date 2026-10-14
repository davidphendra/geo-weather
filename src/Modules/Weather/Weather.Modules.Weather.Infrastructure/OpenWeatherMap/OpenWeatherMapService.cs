using System.Net;
using System.Net.Http.Json;

using global::Weather.Modules.Weather.Application.Abstractions;
using global::Weather.Modules.Weather.Domain.WeatherReports;

using Microsoft.Extensions.Logging;

namespace Weather.Modules.Weather.Infrastructure.OpenWeatherMap;

internal sealed class OpenWeatherMapService(
    HttpClient httpClient,
    ILogger<OpenWeatherMapService> logger) : IWeatherProvider
{
    public async Task<WeatherReport?> GetCurrentWeatherAsync(
        string cityName,
        string? countryCode,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(cityName);

        string city = string.IsNullOrWhiteSpace(countryCode) ? cityName : $"{cityName},{countryCode}";
        string requestUri = $"data/2.5/weather?q={Uri.EscapeDataString(city)}&units=imperial";

        logger.LogDebug("Requesting current weather for {City}", cityName);

        HttpResponseMessage response;
        try
        {
            response = await httpClient.GetAsync(requestUri, cancellationToken);
        }
        catch (HttpRequestException exception)
        {
            throw new WeatherProviderException("Weather provider is unavailable.", exception);
        }

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        if (!response.IsSuccessStatusCode)
        {
            logger.LogWarning(
                "Weather provider returned {StatusCode} for {City}",
                (int)response.StatusCode,
                cityName);

            throw new WeatherProviderException($"Weather provider returned status {(int)response.StatusCode}.");
        }

        OpenWeatherMapResponse? payload = await response.Content
            .ReadFromJsonAsync<OpenWeatherMapResponse>(cancellationToken);

        return payload is null
            ? throw new WeatherProviderException("Weather provider returned an empty body.")
            : OpenWeatherMapMapper.ToDomain(payload);
    }
}
