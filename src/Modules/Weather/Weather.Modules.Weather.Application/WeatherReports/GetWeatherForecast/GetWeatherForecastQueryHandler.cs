using System.Diagnostics;
using System.Diagnostics.Metrics;

using MediatR;

using Weather.BuildingBlocks.Application.Telemetry;
using Weather.Modules.Weather.Application.Abstractions;
using Weather.Modules.Weather.Domain.Forecasts;

namespace Weather.Modules.Weather.Application.WeatherReports.GetWeatherForecast;

internal sealed class GetWeatherForecastQueryHandler(IWeatherProvider weatherProvider)
    : IRequestHandler<GetWeatherForecastQuery, WeatherForecastDto>
{
    public async Task<WeatherForecastDto> Handle(
        GetWeatherForecastQuery request,
        CancellationToken cancellationToken)
    {
        long start = Stopwatch.GetTimestamp();
        string outcome = WeatherMeters.Outcomes.Error;

        try
        {
            WeatherForecast forecast = await weatherProvider.GetForecastAsync(
                request.CityName,
                request.CountryCode,
                cancellationToken)
                ?? throw new WeatherNotFoundException(request.CityName);

            outcome = WeatherMeters.Outcomes.Success;

            return forecast.ToDto();
        }
        catch (WeatherNotFoundException)
        {
            outcome = WeatherMeters.Outcomes.NotFound;
            WeatherMeters.WeatherCityNotFound.Add(1, CountryTag(request.CountryCode));

            throw;
        }
        catch (WeatherProviderException)
        {
            outcome = WeatherMeters.Outcomes.ProviderError;

            throw;
        }
        finally
        {
            TagList tags = new() { { WeatherMeters.Tags.Outcome, outcome } };

            if (!string.IsNullOrWhiteSpace(request.CountryCode))
            {
                tags.Add(WeatherMeters.Tags.CountryCode, request.CountryCode);
            }

            WeatherMeters.WeatherForecastRequests.Add(1, tags);
            WeatherMeters.WeatherForecastDuration.Record(Stopwatch.GetElapsedTime(start).TotalMilliseconds, tags);
        }
    }

    private static TagList CountryTag(string? countryCode) =>
        string.IsNullOrWhiteSpace(countryCode)
            ? default
            : new TagList { { WeatherMeters.Tags.CountryCode, countryCode } };
}
