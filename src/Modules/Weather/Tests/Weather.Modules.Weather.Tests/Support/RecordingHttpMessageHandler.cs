namespace Weather.Modules.Weather.Tests.Support;

/// <summary>
/// Primary <see cref="HttpMessageHandler"/> that returns a prepared sequence of responses (repeating the last
/// one when exhausted) and counts how many attempts reached it. Used to prove that the resilient typed client
/// retries the configured number of times.
/// </summary>
internal sealed class RecordingHttpMessageHandler(params Func<HttpResponseMessage>[] responses) : HttpMessageHandler
{
    private int _calls;

    public int CallCount => Volatile.Read(ref _calls);

    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        int index = Interlocked.Increment(ref _calls) - 1;
        Func<HttpResponseMessage> responder = index < responses.Length ? responses[index] : responses[^1];

        return Task.FromResult(responder());
    }
}
