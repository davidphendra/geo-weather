using System.Reflection;

namespace Weather.ArchTests;

/// <summary>
/// ADR-0014: telemetry primitives live once in BuildingBlocks.Application. Modules and the Domain layer
/// must not take an OpenTelemetry dependency, and Domain must not use metrics/diagnostics at all.
/// </summary>
[Trait("Category", "Observability")]
public sealed class ObservabilityBoundaryTests
{
    [Fact]
    public void DomainAssemblies_DoNotReferenceDiagnosticsOrMetrics()
    {
        Assembly[] domains =
        [
            typeof(Weather.BuildingBlocks.Domain.DomainException).Assembly,
            typeof(Weather.Modules.Geography.Domain.GeographyDomainException).Assembly,
            typeof(Weather.Modules.Weather.Domain.WeatherDomainException).Assembly,
            typeof(Weather.Modules.Journal.Domain.JournalDomainException).Assembly,
        ];

        foreach (Assembly domain in domains)
        {
            string[] forbidden = domain.GetReferencedAssemblies()
                .Select(reference => reference.Name ?? string.Empty)
                .Where(name => name.Equals("System.Diagnostics.DiagnosticSource", StringComparison.Ordinal))
                .ToArray();

            Assert.True(forbidden.Length == 0, $"{domain.GetName().Name} references {string.Join(", ", forbidden)}");
        }
    }

    [Fact]
    public void ModuleAssemblies_DoNotReferenceOpenTelemetry()
    {
        Assembly[] modules =
        [
            typeof(Weather.Modules.Geography.Application.Contracts.IGeographyService).Assembly,
            typeof(Weather.Modules.Weather.Application.Contracts.IWeatherService).Assembly,
            typeof(Weather.Modules.Journal.Application.Contracts.IFavoritesService).Assembly,
        ];

        foreach (Assembly module in modules)
        {
            string[] forbidden = module.GetReferencedAssemblies()
                .Select(reference => reference.Name ?? string.Empty)
                .Where(name =>
                    name.StartsWith("OpenTelemetry", StringComparison.Ordinal)
                    || name.Equals("Microsoft.OpenTelemetry", StringComparison.Ordinal))
                .ToArray();

            Assert.True(forbidden.Length == 0, $"{module.GetName().Name} references {string.Join(", ", forbidden)}");
        }
    }
}
