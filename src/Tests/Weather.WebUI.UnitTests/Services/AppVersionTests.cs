using Weather.WebUI.Services;

namespace Weather.WebUI.UnitTests.Services;

/// <summary>
/// The build identity must be readable and never throw. These tests pin the parse contract for the
/// stamped informational version and prove the value comes from the build stamp, not the assembly identity.
/// </summary>
[Trait("Category", "AppVersion")]
public sealed class AppVersionTests
{
    [Fact]
    public void Parses_prerelease_and_commit()
    {
        AppVersion version = AppVersion.Parse("0.1.24-alpha+g9a7eb6c819");

        Assert.Equal("0.1.24-alpha", version.Version);
        Assert.Equal("9a7eb6c819", version.Commit);
        Assert.Equal("0.1.24-alpha+g9a7eb6c819", version.InformationalVersion);
    }

    [Theory]
    [InlineData("0.1.24", "0.1.24", "")]
    [InlineData("0.1.24+abc123", "0.1.24", "abc123")]
    [InlineData("0.1.24-alpha+abc123", "0.1.24-alpha", "abc123")]
    [InlineData("1.2.3-alpha.4", "1.2.3-alpha.4", "")]
    public void Parses_supported_shapes(string input, string expectedVersion, string expectedCommit)
    {
        AppVersion version = AppVersion.Parse(input);

        Assert.Equal(expectedVersion, version.Version);
        Assert.Equal(expectedCommit, version.Commit);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("garbage")]
    public void Falls_back_without_throwing(string? input)
    {
        AppVersion version = AppVersion.Parse(input);

        Assert.Equal(AppVersion.Unknown, version.Version);
        Assert.Equal(AppVersion.Unknown, version.InformationalVersion);
        Assert.Equal(string.Empty, version.Commit);
    }

    [Fact]
    public void Current_reads_the_build_stamp_not_the_assembly_identity()
    {
        AppVersion version = AppVersion.Current;

        Assert.NotEqual(AppVersion.Unknown, version.Version);
        // AssemblyVersion is major.minor (0.1.0.0); the informational version must differ from it.
        Assert.NotEqual("0.1.0.0", version.Version);
    }
}
