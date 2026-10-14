namespace Weather.Modules.Favorites.Domain.Countries;

/// <summary>An ISO-3166 alpha-2 country code, normalized to upper case.</summary>
public sealed record CountryCode
{
    public string Value { get; }

    private CountryCode(string value) => Value = value;

    public static CountryCode From(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);

        string normalized = value.Trim().ToUpperInvariant();

        return normalized.Length != 2 || !normalized.All(char.IsAsciiLetterUpper)
            ? throw new FavoritesDomainException($"'{value}' is not a valid country code.")
            : new CountryCode(normalized);
    }

    public override string ToString() => Value;
}
