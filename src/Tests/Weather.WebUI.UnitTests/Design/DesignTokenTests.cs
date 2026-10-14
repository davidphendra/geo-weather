namespace Weather.WebUI.UnitTests.Design;

/// <summary>
/// Guards the Telemetry Slate design tokens. The Tailwind config is the single source of truth;
/// the compiled CSS is checked for the utilities the shell actually uses (JIT emits used classes only).
/// </summary>
[Trait("Category", "DesignTokens")]
public sealed class DesignTokenTests
{
    private static readonly string Config =
        File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "tailwind.config.js"));

    private static readonly string Css =
        File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "app.css"));

    [Theory]
    [InlineData("#131b2e")]  // primary-container
    [InlineData("#006398")]  // secondary
    [InlineData("#f8f9ff")]  // surface
    [InlineData("#5bb8fe")]  // secondary-container
    [InlineData("#0b1c30")]  // on-surface
    [InlineData("#c4e7ff")]  // tertiary-fixed
    [InlineData("0.125rem")] // radius DEFAULT
    [InlineData("1.5rem")]   // space-xl
    [InlineData("2rem")]     // margin-desktop
    [InlineData("Geist")]
    [InlineData("JetBrains Mono")]
    public void Config_declares_token(string token) => Assert.Contains(token, Config);

    [Theory]
    [InlineData("bg-primary-container")]
    [InlineData("font-code-sm")]
    [InlineData("text-on-surface")]
    [InlineData("icon-fill")]
    public void Compiled_css_contains_shell_utilities(string utility) => Assert.Contains(utility, Css);
}
