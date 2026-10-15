namespace Weather.WebUI.Services;

/// <summary>Client mirror of the Weather five-day forecast contract.</summary>
public sealed record WeatherForecastModel(
    LocationModel Location,
    IReadOnlyList<ForecastDayModel> Days);
