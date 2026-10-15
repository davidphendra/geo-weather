namespace Weather.Modules.Journal.Domain.Cities;

/// <summary>Identity of a city (from Geography's integration event).</summary>
public sealed record CityId
{
    public Guid Value { get; }

    private CityId(Guid value) => Value = value;

    public static CityId From(Guid value) => value == Guid.Empty
        ? throw new JournalDomainException("City id cannot be empty.")
        : new CityId(value);
}
