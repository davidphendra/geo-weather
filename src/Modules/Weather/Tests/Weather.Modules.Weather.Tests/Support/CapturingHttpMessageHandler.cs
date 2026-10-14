using System.Net;

namespace Weather.Modules.Weather.Tests.Support;

internal sealed class CapturingHttpMessageHandler : HttpMessageHandler
{
    public Uri? LastRequestUri { get; private set; }

    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        LastRequestUri = request.RequestUri;

        return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
    }
}
