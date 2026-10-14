namespace Weather.Modules.Geography.Application.Countries.GetCountries;

/// <summary>Filter for the country catalog. A null/empty term means no predicate.</summary>
public sealed record CountryQuery(string? SearchTerm = null);
