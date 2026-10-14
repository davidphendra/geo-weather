namespace Weather.WebUI.Services;

/// <summary>Outcome of a write request: the real status, the parsed resource and the raw body.</summary>
public sealed record ApiResult<T>(int Status, bool IsSuccess, T? Value, string RawJson);
