using System.Reflection;

using Weather.Modules.Notes.Application.Contracts;

namespace Weather.ArchTests;

/// <summary>Notes may reference another module only through its IntegrationEvents assembly (ADR-0005/0006).</summary>
[Trait("Category", "ModuleIsolation")]
public sealed class ModuleIsolationTests
{
    [Fact]
    public void NotesReferencesOnlyGeographyIntegrationEvents()
    {
        Assembly notes = typeof(INotesModule).Assembly;

        string[] forbidden = notes.GetReferencedAssemblies()
            .Select(reference => reference.Name ?? string.Empty)
            .Where(name => name.StartsWith("Weather.Modules.", StringComparison.Ordinal))
            .Where(name => !name.StartsWith("Weather.Modules.Notes.", StringComparison.Ordinal))
            .Where(name => !name.Equals("Weather.Modules.Geography.IntegrationEvents", StringComparison.Ordinal))
            .ToArray();

        Assert.Empty(forbidden);
    }
}
