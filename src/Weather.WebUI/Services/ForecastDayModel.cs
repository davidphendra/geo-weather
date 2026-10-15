namespace Weather.WebUI.Services;

/// <summary>One forecast day: the day's high, low, rain chance, and sky.</summary>
public sealed record ForecastDayModel(
    DateOnly Date,
    TemperatureModel High,
    TemperatureModel Low,
    int PrecipitationProbabilityPercent,
    string SkyCondition);
