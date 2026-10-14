using global::Weather.Modules.Weather.Application.Abstractions;
using global::Weather.Modules.Weather.Domain;
using global::Weather.Modules.Weather.Domain.WeatherReports;

namespace Weather.Modules.Weather.Infrastructure.OpenWeatherMap;

internal static class OpenWeatherMapMapper
{
    private const decimal MilesPerHourToMetersPerSecond = 0.44704m;

    public static WeatherReport ToDomain(OpenWeatherMapResponse response)
    {
        ArgumentNullException.ThrowIfNull(response);

        try
        {
            return WeatherReport.Create(
                Location.Of(response.Name, response.Sys.Country),
                DateTimeOffset.FromUnixTimeSeconds(response.Dt),
                Temperature.FromFahrenheit((decimal)response.Main.Temp),
                RelativeHumidity.FromPercent(response.Main.Humidity),
                WindInfo.FromMetersPerSecond(
                    (decimal)response.Wind.Speed * MilesPerHourToMetersPerSecond,
                    response.Wind.Deg),
                Visibility.FromMeters(response.Visibility),
                Pressure.FromHectopascals((decimal)response.Main.Pressure),
                SkyCondition.FromProvider(response.Weather.Count > 0 ? response.Weather[0].Main : null));
        }
        catch (WeatherDomainException exception)
        {
            // Malformed upstream data is a provider fault, not an application fault.
            throw new WeatherProviderException("Weather provider returned invalid data.", exception);
        }
    }
}
