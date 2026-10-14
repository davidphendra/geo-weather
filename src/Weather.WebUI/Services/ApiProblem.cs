namespace Weather.WebUI.Services;

/// <summary>RFC 9457 Problem Details as returned by the API, including the stable code.</summary>
public sealed record ApiProblem(string? Type, string? Title, int Status, string? Detail, string? Code);
