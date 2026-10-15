using global::Weather.Modules.Weather.Application.Abstractions;
using global::Weather.Modules.Weather.Domain.Forecasts;
using global::Weather.Modules.Weather.Domain.WeatherReports;

namespace Weather.IntegrationTests.Support;

/// <summary>Deterministic in-memory replacement for the OpenWeatherMap adapter. No network.</summary>
internal sealed class StubWeatherProvider : IWeatherProvider
{
    public Task<WeatherReport?> GetCurrentWeatherAsync(
        string cityName,
        string? countryCode,
        CancellationToken cancellationToken)
    {
        if (string.Equals(cityName, "ProviderDown", StringComparison.OrdinalIgnoreCase))
        {
            throw new WeatherProviderException("Weather provider is unavailable.");
        }

        if (string.Equals(cityName, "Nowhere", StringComparison.OrdinalIgnoreCase))
        {
            return Task.FromResult<WeatherReport?>(null);
        }

        WeatherReport report = WeatherReport.Create(
            Location.Of(cityName, countryCode ?? "GB"),
            Coordinates.Of(51.5074m, -0.1278m),
            DateTimeOffset.UnixEpoch,
            Temperature.FromFahrenheit(50m),
            RelativeHumidity.FromPercent(80),
            WindInfo.FromMetersPerSecond(4m, 220),
            Visibility.FromMeters(10000m),
            Pressure.FromHectopascals(1013m),
            SkyCondition.Cloudy);

        return Task.FromResult<WeatherReport?>(report);
    }

    public Task<WeatherForecast?> GetForecastAsync(
        string cityName,
        string? countryCode,
        CancellationToken cancellationToken)
    {
        if (string.Equals(cityName, "ProviderDown", StringComparison.OrdinalIgnoreCase))
        {
            throw new WeatherProviderException("Weather provider is unavailable.");
        }

        if (string.Equals(cityName, "Nowhere", StringComparison.OrdinalIgnoreCase))
        {
            return Task.FromResult<WeatherForecast?>(null);
        }

        ForecastInterval[] intervals =
        [
            .. Enumerable.Range(0, 5).Select(offset => ForecastInterval.Create(
                DateTimeOffset.UnixEpoch.AddDays(offset),
                Temperature.FromFahrenheit(50m + offset),
                PrecipitationProbability.FromPercent(20 + offset),
                SkyCondition.Cloudy)),
        ];

        WeatherForecast forecast = WeatherForecast.FromIntervals(
            Location.Of(cityName, countryCode ?? "GB"),
            intervals,
            TimeSpan.Zero);

        return Task.FromResult<WeatherForecast?>(forecast);
    }
}
