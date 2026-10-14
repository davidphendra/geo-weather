namespace Weather.WebUI.Services;

/// <summary>A failed API call carrying the RFC 9457 problem returned by the host.</summary>
public sealed class WeatherApiException : Exception
{
    public WeatherApiException(ApiProblem problem)
        : base(problem?.Detail ?? problem?.Title ?? "The weather API call failed.")
    {
        ArgumentNullException.ThrowIfNull(problem);
        Problem = problem;
    }

    /// <summary>Structured problem details (status and stable code) for the UI to render.</summary>
    public ApiProblem Problem { get; }
}
