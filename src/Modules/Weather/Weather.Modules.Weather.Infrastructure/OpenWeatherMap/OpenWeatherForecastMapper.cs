using Weather.Modules.Weather.Application.Abstractions;
using Weather.Modules.Weather.Domain;
using Weather.Modules.Weather.Domain.Forecasts;
using Weather.Modules.Weather.Domain.WeatherReports;

namespace Weather.Modules.Weather.Infrastructure.OpenWeatherMap;

internal static class OpenWeatherForecastMapper
{
    public static WeatherForecast ToDomain(OpenWeatherForecastResponse response)
    {
        ArgumentNullException.ThrowIfNull(response);

        try
        {
            ForecastInterval[] intervals =
            [
                .. response.List.Select(step => ForecastInterval.Create(
                    DateTimeOffset.FromUnixTimeSeconds(step.Dt),
                    Temperature.FromFahrenheit((decimal)step.Main.Temp),
                    PrecipitationProbability.FromRatio(step.Pop),
                    SkyCondition.FromProvider(step.Weather.Count > 0 ? step.Weather[0].Main : null))),
            ];

            return WeatherForecast.FromIntervals(
                Location.Of(response.City.Name, response.City.Country),
                intervals,
                TimeSpan.FromSeconds(response.City.Timezone));
        }
        catch (WeatherDomainException exception)
        {
            // Malformed upstream data is a provider fault, not an application fault.
            throw new WeatherProviderException("Weather provider returned invalid forecast data.", exception);
        }
    }
}
