namespace Weather.WebUI.Configuration;

/// <summary>
/// Configurable storage location for the per-module SQLite files ("Database" configuration section).
/// The directory is relative to the content root unless it is an absolute path.
/// </summary>
public sealed class DatabaseOptions
{
    /// <summary>Name of the configuration section.</summary>
    public const string SectionName = "Database";

    /// <summary>Directory holding the SQLite files; absolute, or relative to the content root.</summary>
    public string DataDirectory { get; set; } = string.Empty;

    /// <summary>Connection-string name (for example "Geography") to SQLite file name in <see cref="DataDirectory"/>.</summary>
    public Dictionary<string, string> Files { get; set; } = [];
}
