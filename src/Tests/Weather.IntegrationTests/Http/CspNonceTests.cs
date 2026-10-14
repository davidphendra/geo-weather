using Weather.IntegrationTests.Support;

namespace Weather.IntegrationTests.Http;

/// <summary>The request nonce in the header matches the rendered Blazor script nonces (e06s06).</summary>
[Trait("Category", "CspNonce")]
public sealed class CspNonceTests(WeatherHostFactory factory) : IClassFixture<WeatherHostFactory>
{
    [Fact]
    public async Task Header_nonce_matches_the_rendered_scripts()
    {
        HttpResponseMessage response = await factory.CreateClient().GetAsync("/");

        Assert.True(response.Headers.TryGetValues("Content-Security-Policy", out IEnumerable<string>? values));
        string policy = string.Join("; ", values ?? []);

        int start = policy.IndexOf("'nonce-", StringComparison.Ordinal);
        Assert.True(start >= 0, "The policy must carry a nonce.");

        string nonce = policy[(start + "'nonce-".Length)..];
        nonce = nonce[..nonce.IndexOf('\'')];
        Assert.False(string.IsNullOrWhiteSpace(nonce));

        string html = await response.Content.ReadAsStringAsync();

        Assert.Contains($"nonce=\"{nonce}\"", html);
        Assert.Contains("_framework/blazor.web", html);
    }
}
