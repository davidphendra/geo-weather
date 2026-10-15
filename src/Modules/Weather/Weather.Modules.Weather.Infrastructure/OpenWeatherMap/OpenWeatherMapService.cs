using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Net;
using System.Net.Http.Json;

using global::Weather.BuildingBlocks.Application.Telemetry;
using global::Weather.Modules.Weather.Application.Abstractions;
using global::Weather.Modules.Weather.Domain.Forecasts;
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

        Uri requestUri = RequestUri("data/2.5/weather", cityName, countryCode);

        logger.LogDebug("Requesting current weather for {City}", cityName);

        OpenWeatherMapResponse? payload = await GetPayloadAsync<OpenWeatherMapResponse>(
            requestUri,
            cityName,
            cancellationToken);

        return payload is null ? null : OpenWeatherMapMapper.ToDomain(payload);
    }

    public async Task<WeatherForecast?> GetForecastAsync(
        string cityName,
        string? countryCode,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(cityName);

        using Activity? activity = WeatherActivity.Source.StartActivity("weather.provider.get_forecast");
        activity?.SetTag(WeatherMeters.Tags.CountryCode, countryCode);

        Uri requestUri = RequestUri("data/2.5/forecast", cityName, countryCode);

        logger.LogDebug("Requesting forecast for {City}", cityName);

        OpenWeatherForecastResponse? payload = await GetPayloadAsync<OpenWeatherForecastResponse>(
            requestUri,
            cityName,
            cancellationToken);

        return payload is null ? null : OpenWeatherForecastMapper.ToDomain(payload);
    }

    private static Uri RequestUri(string path, string cityName, string? countryCode)
    {
        string city = string.IsNullOrWhiteSpace(countryCode) ? cityName : $"{cityName},{countryCode}";

        return new Uri($"{path}?q={Uri.EscapeDataString(city)}&units=imperial", UriKind.Relative);
    }

    private async Task<TPayload?> GetPayloadAsync<TPayload>(
        Uri requestUri,
        string cityName,
        CancellationToken cancellationToken)
        where TPayload : class
    {
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

        TPayload? payload = await response.Content.ReadFromJsonAsync<TPayload>(cancellationToken);

        if (payload is null)
        {
            RecordFailure(WeatherMeters.Outcomes.EmptyBody);

            throw new WeatherProviderException("Weather provider returned an empty body.");
        }

        return payload;
    }

    private static void RecordFailure(string errorType) =>
        WeatherMeters.WeatherProviderFailures.Add(
            1,
            new TagList { { WeatherMeters.Tags.ErrorType, errorType } });
}
