using Microsoft.Data.Sqlite;

namespace Weather.WebUI.Configuration;

/// <summary>
/// Ensures the directory of a SQLite connection string exists before the database is opened. SQLite creates the
/// database file but not its parent directory, so a configured path (for example a mounted volume) would
/// otherwise fail with "unable to open database file".
/// </summary>
internal static class SqliteConnectionDirectory
{
    public static void EnsureCreated(string? connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            return;
        }

        SqliteConnectionStringBuilder builder;
        try
        {
            builder = new SqliteConnectionStringBuilder(connectionString);
        }
        catch (ArgumentException)
        {
            // Not a SQLite connection string; nothing to prepare.
            return;
        }

        if (string.IsNullOrWhiteSpace(builder.DataSource)
            || builder.DataSource == ":memory:"
            || builder.Mode == SqliteOpenMode.Memory)
        {
            return;
        }

        string? directory = Path.GetDirectoryName(Path.GetFullPath(builder.DataSource));
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }
    }
}
