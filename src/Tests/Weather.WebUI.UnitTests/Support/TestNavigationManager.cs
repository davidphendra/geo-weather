using Microsoft.AspNetCore.Components;

namespace Weather.WebUI.UnitTests.Support;

/// <summary>A <see cref="NavigationManager"/> with a fixed base URI for server-side client tests.</summary>
internal sealed class TestNavigationManager : NavigationManager
{
    public TestNavigationManager(string baseUri = "https://test.local/") => Initialize(baseUri, baseUri);
}
