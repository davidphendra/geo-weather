using System.Diagnostics.Metrics;
using System.Net;

using Weather.BuildingBlocks.Application.Telemetry;
using Weather.IntegrationTests.Support;

namespace Weather.IntegrationTests.Http;

[Trait("Category", "Observability")]
public sealed class ApiErrorMetricsTests(WeatherHostFactory factory) : IClassFixture<WeatherHostFactory>
{
    [Fact]
    public async Task NotFound_RecordsApiErrorWithCodeAndRoute()
    {
        List<(string Code, string Route)> errors = [];

        using MeterListener listener = new();
        listener.SetMeasurementEventCallback<long>((instrument, _, tags, _) =>
        {
            if (instrument.Name != "api.errors")
            {
                return;
            }

            string code = string.Empty;
            string route = string.Empty;

            foreach (KeyValuePair<string, object?> tag in tags)
            {
                if (tag.Key == WeatherMeters.Tags.ErrorCode)
                {
                    code = tag.Value as string ?? string.Empty;
                }

                if (tag.Key == WeatherMeters.Tags.Route)
                {
                    route = tag.Value as string ?? string.Empty;
                }
            }

            lock (errors)
            {
                errors.Add((code, route));
            }
        });
        listener.EnableMeasurementEvents(WeatherMeters.ApiErrors);
        listener.Start();

        HttpResponseMessage response = await factory.CreateClient()
            .GetAsync("/api/countries/ZZ/cities");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

        lock (errors)
        {
            Assert.Contains(errors, error => error.Code == "RESOURCE_NOT_FOUND");
        }
    }
}
