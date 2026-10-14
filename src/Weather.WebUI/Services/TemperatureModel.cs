namespace Weather.WebUI.Services;

/// <summary>A temperature in both units.</summary>
public sealed record TemperatureModel(decimal Celsius, decimal Fahrenheit);
