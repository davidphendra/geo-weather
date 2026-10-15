using System.Reflection;

namespace Weather.WebUI.Services;

/// <summary>
/// The running application's build identity, read from the assembly's stamped informational version
/// (<c>major.minor.patch[-prerelease][+commit]</c>, produced by Nerdbank.GitVersioning). Unlike the
/// assembly identity version, this changes with every build, so it identifies the deployed artifact.
/// Parsing never throws: an unreadable value degrades to <see cref="Unknown"/>.
/// </summary>
internal sealed record AppVersion(string Version, string InformationalVersion, string Commit)
{
    /// <summary>Rendered when no usable build metadata is present (tests, tooling, a broken stamp).</summary>
    public const string Unknown = "0.0.0-unknown";

    /// <summary>The version of the assembly that owns this type (the WebUI host).</summary>
    public static AppVersion Current { get; } = FromAssembly(typeof(AppVersion).Assembly);

    /// <summary>Reads and parses the informational version of <paramref name="assembly"/>.</summary>
    public static AppVersion FromAssembly(Assembly assembly)
    {
        ArgumentNullException.ThrowIfNull(assembly);

        string? informational = assembly
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()
            ?.InformationalVersion;

        return Parse(informational);
    }

    /// <summary>Parses <c>{semver}[+{commit}]</c>. The commit may carry a historical leading <c>g</c>.</summary>
    public static AppVersion Parse(string? informationalVersion)
    {
        if (string.IsNullOrWhiteSpace(informationalVersion))
        {
            return UnknownVersion();
        }

        int separator = informationalVersion.IndexOf('+', StringComparison.Ordinal);
        string version = separator < 0 ? informationalVersion : informationalVersion[..separator];
        string commit = separator < 0 ? string.Empty : informationalVersion[(separator + 1)..];

        if (commit.StartsWith('g'))
        {
            commit = commit[1..];
        }

        return LooksLikeSemanticVersion(version)
            ? new AppVersion(version, informationalVersion, commit)
            : UnknownVersion();
    }

    private static AppVersion UnknownVersion() => new(Unknown, Unknown, string.Empty);

    // A defensive shape check only: the value must start with a digit and contain a dot. Full SemVer
    // validation is unnecessary because the only trusted source is the build stamp itself.
    private static bool LooksLikeSemanticVersion(string value) =>
        value.Length > 0 && char.IsAsciiDigit(value[0]) && value.Contains('.', StringComparison.Ordinal);
}
