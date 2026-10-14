using System.Reflection;

using Weather.BuildingBlocks.Application.Events;
using Weather.BuildingBlocks.Domain;

namespace Weather.ArchTests;

/// <summary>
/// Pins the public surface of BuildingBlocks so it cannot become a dumping ground (e01s01 risk / e01s04 stop
/// condition). Adding a public type is a deliberate decision: extend the allowlist and say why.
/// </summary>
[Trait("Category", "BuildingBlocksBoundary")]
public sealed class BuildingBlocksBoundaryTests
{
    [Fact]
    public void BuildingBlocksDomain_exposes_only_the_agreed_kernel()
    {
        string[] expected = ["CountryCode", "DomainException", "GeoCoordinate"];

        Assert.Equal(expected, PublicTypeNames(typeof(DomainException).Assembly));
    }

    [Fact]
    public void BuildingBlocksApplication_exposes_only_shared_mechanism()
    {
        string[] expected =
        [
            "IDateTimeProvider",
            "IInMemoryEventsBus",
            "IIntegrationEvent",
            "IIntegrationEventHandler`1",
            // ADR-0014: shared telemetry primitives (instruments + activity source + tag/outcome catalogs).
            "Outcomes",
            "Tags",
            "TelemetryBehavior`2",
            "ValidationBehaviorRegistration",
            "ValidationBehavior`2",
            "WeatherActivity",
            "WeatherMeters",
        ];

        Assert.Equal(expected, PublicTypeNames(typeof(IIntegrationEvent).Assembly));
    }

    private static string[] PublicTypeNames(Assembly assembly) =>
        assembly.GetExportedTypes()
            .Select(type => type.Name)
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToArray();
}
