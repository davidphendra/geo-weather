using Microsoft.Extensions.DependencyInjection;

using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;

using Weather.IntegrationTests.Support;

namespace Weather.IntegrationTests.Http;

[Trait("Category", "Observability")]
public sealed class ObservabilityTests(WeatherHostFactory factory) : IClassFixture<WeatherHostFactory>
{
    [Fact]
    public void TelemetryDisabled_DoesNotRegisterAProvider()
    {
        // WeatherHostFactory sets Observability:Enabled=false — nothing may construct an exporter.
        Assert.Null(factory.Services.GetService<MeterProvider>());
        Assert.Null(factory.Services.GetService<TracerProvider>());
    }
}
