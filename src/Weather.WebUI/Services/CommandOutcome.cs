namespace Weather.WebUI.Services;

/// <summary>The real status and body returned by a write request, for the response well.</summary>
public sealed record CommandOutcome(int Status, bool IsSuccess, string RawJson);
