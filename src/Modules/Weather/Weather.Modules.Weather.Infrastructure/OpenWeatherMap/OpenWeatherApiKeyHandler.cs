using Microsoft.Extensions.Options;

namespace Weather.Modules.Weather.Infrastructure.OpenWeatherMap;

/// <summary>Attaches the API key so it never appears in logs, traces, or exception messages.</summary>
internal sealed class OpenWeatherApiKeyHandler(IOptions<OpenWeatherOptions> options) : DelegatingHandler
{
    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        string separator = string.IsNullOrEmpty(request.RequestUri!.Query) ? "?" : "&";
        request.RequestUri = new Uri($"{request.RequestUri}{separator}appid={options.Value.ApiKey}");

        return base.SendAsync(request, cancellationToken);
    }
}
