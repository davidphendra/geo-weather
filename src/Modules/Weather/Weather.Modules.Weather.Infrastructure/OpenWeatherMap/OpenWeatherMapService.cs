using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Net;
using System.Net.Http.Json;

using global::Weather.BuildingBlocks.Application.Telemetry;
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

        using Activity? activity = WeatherActivity.Source.StartActivity("weather.provider.get_current");
        activity?.SetTag(WeatherMeters.Tags.CountryCode, countryCode);

        string city = string.IsNullOrWhiteSpace(countryCode) ? cityName : $"{cityName},{countryCode}";
        Uri requestUri = new($"data/2.5/weather?q={Uri.EscapeDataString(city)}&units=imperial", UriKind.Relative);

        logger.LogDebug("Requesting current weather for {City}", cityName);

        long start = Stopwatch.GetTimestamp();

        HttpResponseMessage response;
        try
        {
            response = await httpClient.GetAsync(requestUri, cancellationToken);
        }
        catch (HttpRequestException exception)
        {
            RecordFailure(WeatherMeters.Outcomes.Unavailable);
            throw new WeatherProviderException("Weather provider is unavailable.", exception);
        }
        catch (TaskCanceledException exception) when (!cancellationToken.IsCancellationRequested)
        {
            RecordFailure(WeatherMeters.Outcomes.Timeout);
            throw new WeatherProviderException("Weather provider timed out.", exception);
        }

        TagList tags = new() { { WeatherMeters.Tags.StatusCode, (int)response.StatusCode } };
        WeatherMeters.WeatherProviderRequests.Add(1, tags);
        WeatherMeters.WeatherProviderDuration.Record(Stopwatch.GetElapsedTime(start).TotalMilliseconds, tags);

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

            RecordFailure(WeatherMeters.Outcomes.BadStatus);

            throw new WeatherProviderException($"Weather provider returned status {(int)response.StatusCode}.");
        }

        OpenWeatherMapResponse? payload = await response.Content
            .ReadFromJsonAsync<OpenWeatherMapResponse>(cancellationToken);

        if (payload is null)
        {
            RecordFailure(WeatherMeters.Outcomes.EmptyBody);

            throw new WeatherProviderException("Weather provider returned an empty body.");
        }

        return OpenWeatherMapMapper.ToDomain(payload);
    }

    private static void RecordFailure(string errorType) =>
        WeatherMeters.WeatherProviderFailures.Add(
            1,
            new TagList { { WeatherMeters.Tags.ErrorType, errorType } });
}
