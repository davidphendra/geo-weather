namespace Weather.Modules.Weather.Domain.WeatherReports;

/// <summary>A city and its country code.</summary>
public sealed record Location
{
    public string City { get; }

    public string CountryCode { get; }

    private Location(string city, string countryCode)
    {
        City = city;
        CountryCode = countryCode;
    }

    public static Location Of(string city, string countryCode)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(city);
        ArgumentException.ThrowIfNullOrWhiteSpace(countryCode);

        return new Location(city, countryCode);
    }
}
