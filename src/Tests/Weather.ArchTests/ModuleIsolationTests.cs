using System.Reflection;

using Weather.Modules.Journal.Application.Contracts;

namespace Weather.ArchTests;

/// <summary>
/// The Journal module (merged Favorites + Notes) may reference another module only through its
/// IntegrationEvents assembly (ADR-0005/0006).
/// </summary>
[Trait("Category", "ModuleIsolation")]
public sealed class ModuleIsolationTests
{
    [Fact]
    public void JournalReferencesOnlyGeographyIntegrationEvents()
    {
        Assembly journal = typeof(IFavoritesService).Assembly;

        string[] forbidden = journal.GetReferencedAssemblies()
            .Select(reference => reference.Name ?? string.Empty)
            .Where(name => name.StartsWith("Weather.Modules.", StringComparison.Ordinal))
            .Where(name => !name.StartsWith("Weather.Modules.Journal.", StringComparison.Ordinal))
            .Where(name => !name.Equals("Weather.Modules.Geography.IntegrationEvents", StringComparison.Ordinal))
            .ToArray();

        Assert.Empty(forbidden);
    }
}
