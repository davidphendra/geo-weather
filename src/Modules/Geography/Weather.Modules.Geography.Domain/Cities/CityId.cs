namespace Weather.Modules.Geography.Domain.Cities;

/// <summary>Identity of a city.</summary>
public sealed record CityId
{
    public Guid Value { get; }

    private CityId(Guid value) => Value = value;

    public static CityId From(Guid value) => value == Guid.Empty
        ? throw new GeographyDomainException("City id cannot be empty.")
        : new CityId(value);

    public static CityId New() => new(Guid.NewGuid());
}
