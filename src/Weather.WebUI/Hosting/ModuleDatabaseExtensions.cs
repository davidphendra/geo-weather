using Microsoft.Extensions.Configuration;

using Weather.WebUI.Configuration;

namespace Weather.WebUI.Hosting;

/// <summary>
/// Resolves the per-module SQLite connection strings from the "Database" section and creates their
/// directories. SQLite files live outside wwwroot so they are never addressable over HTTP, and an
/// explicit ConnectionStrings entry always wins over the configured default (ADR-0013).
/// </summary>
internal static class ModuleDatabaseExtensions
{
    public static WebApplicationBuilder AddModuleDatabases(this WebApplicationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        DatabaseOptions database = builder.Configuration
            .GetSection(DatabaseOptions.SectionName)
            .Get<DatabaseOptions>() ?? new DatabaseOptions();

        if (!string.IsNullOrWhiteSpace(database.DataDirectory) && database.Files.Count > 0)
        {
            string dataDirectory = Path.IsPathRooted(database.DataDirectory)
                ? database.DataDirectory
                : Path.Combine(builder.Environment.ContentRootPath, database.DataDirectory);

            Dictionary<string, string?> defaultSqliteConnections = database.Files
                .Where(file => string.IsNullOrWhiteSpace(builder.Configuration.GetConnectionString(file.Key)))
                .ToDictionary(
                    file => $"ConnectionStrings:{file.Key}",
                    file => (string?)$"Data Source={Path.Combine(dataDirectory, file.Value)}");

            if (defaultSqliteConnections.Count > 0)
            {
                Directory.CreateDirectory(dataDirectory);
                builder.Configuration.AddInMemoryCollection(defaultSqliteConnections);
            }
        }

        // Create the directory for every resolved SQLite connection string (configured default or explicit
        // override), so SQLite never fails with "unable to open database file" on a fresh deployment.
        foreach (IConfigurationSection connection in builder.Configuration.GetSection("ConnectionStrings").GetChildren())
        {
            SqliteConnectionDirectory.EnsureCreated(connection.Value);
        }

        return builder;
    }
}
