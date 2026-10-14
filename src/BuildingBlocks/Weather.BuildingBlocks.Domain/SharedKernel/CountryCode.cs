namespace Weather.BuildingBlocks.Domain.SharedKernel;

/// <summary>An ISO-3166 alpha-2 country code, normalized to upper case.</summary>
public sealed record CountryCode
{
    public string Value { get; }

    private CountryCode(string value) => Value = value;

    public static CountryCode From(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);

        string normalized = value.Trim().ToUpperInvariant();

        return normalized.Length == 2 && normalized.All(char.IsAsciiLetterUpper)
            ? new CountryCode(normalized)
            : throw new DomainException($"'{value}' is not an ISO-3166 alpha-2 country code.");
    }

    public override string ToString() => Value;
}
