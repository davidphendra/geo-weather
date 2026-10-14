namespace Weather.Modules.Geography.Domain.Countries;

/// <summary>A country in the catalog.</summary>
public sealed class Country
{
    public CountryCode Code { get; }

    public string Name { get; }

    private Country(CountryCode code, string name)
    {
        Code = code;
        Name = name;
    }

    public static Country Create(CountryCode code, string name)
    {
        ArgumentNullException.ThrowIfNull(code);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        return new Country(code, name);
    }
}
