using System.Reflection;

namespace Weather.Modules.Geography.Infrastructure.Migrations.Seed;

/// <summary>Loads embedded seed scripts used by the data migrations.</summary>
internal static class SeedScripts
{
    public static string Countries => Read("countries.sql");

    public static string Cities => Read("cities.sql");

    private static string Read(string fileName)
    {
        string resourceName = $"Weather.Modules.Geography.Infrastructure.Migrations.Seed.{fileName}";

        using Stream? stream = typeof(SeedScripts).Assembly.GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException($"Embedded seed script '{resourceName}' was not found.");

        using StreamReader reader = new(stream);

        return reader.ReadToEnd();
    }
}
