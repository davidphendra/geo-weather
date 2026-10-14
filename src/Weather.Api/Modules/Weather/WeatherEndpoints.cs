using global::Weather.Modules.Weather.Application.Contracts;
using global::Weather.Modules.Weather.Application.WeatherReports.GetCurrentWeather;

using Microsoft.AspNetCore.Http.HttpResults;

namespace Weather.Api.Modules.Weather;

internal static class WeatherEndpoints
{
    public static IEndpointRouteBuilder MapWeatherEndpoints(this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        endpoints.MapGet(
                "/api/weather/{cityName}",
                async Task<Results<Ok<WeatherReportDto>, NotFound>> (
                    string cityName,
                    string? countryCode,
                    IWeatherModule weatherModule,
                    CancellationToken cancellationToken) =>
                {
                    WeatherReportDto? report = await weatherModule.GetCurrentWeatherAsync(
                        cityName,
                        countryCode,
                        cancellationToken);

                    return report is null ? TypedResults.NotFound() : TypedResults.Ok(report);
                })
            .WithName("GetCurrentWeather")
            .WithSummary("Get current weather for a city.");

        return endpoints;
    }
}
