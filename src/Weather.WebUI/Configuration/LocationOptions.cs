namespace Weather.WebUI.Configuration;

/// <summary>
/// Default location the Weather Explorer opens on ("Location" configuration section). A blank or
/// unknown country/city falls back to the first entry in the catalog, so the page never opens empty.
/// </summary>
public sealed class LocationOptions
{
    public const string SectionName = "Location";

    /// <summary>ISO-3166-1 alpha-2 code of the country selected on first load.</summary>
    public string DefaultCountryCode { get; set; } = "ID";

    /// <summary>City selected on first load when <see cref="DefaultCountryCode"/> is shown.</summary>
    public string DefaultCityName { get; set; } = "Jakarta";
}
