namespace Weather.WebUI.Services;

/// <summary>Wind speed, direction and cardinal label.</summary>
public sealed record WindModel(decimal SpeedMetersPerSecond, int? DirectionDegrees, string Cardinal);
