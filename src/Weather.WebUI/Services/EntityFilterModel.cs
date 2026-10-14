namespace Weather.WebUI.Services;

/// <summary>
/// Search, country and type state for the entity table. Only favorites are listable by the API
/// (the Notes module is POST-only), so the type control has a single real state and is kept for
/// design parity (D3).
/// </summary>
public sealed class EntityFilterModel
{
    public IReadOnlyList<FavoriteCityModel> All { get; private set; } = [];

    public string Search { get; set; } = string.Empty;

    public string Country { get; set; } = "ALL";

    public EntityTypeFilter Type { get; set; } = EntityTypeFilter.All;

    public IReadOnlyList<string> Countries =>
    [
        .. All
            .Select(favorite => favorite.CountryCode)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Order(StringComparer.OrdinalIgnoreCase),
    ];

    public IReadOnlyList<FavoriteCityModel> Visible =>
    [
        .. All
            .Where(MatchesCountry)
            .Where(MatchesSearch),
    ];

    public void Set(IReadOnlyList<FavoriteCityModel> rows) => All = rows;

    private bool MatchesCountry(FavoriteCityModel favorite) =>
        Country == "ALL" || string.Equals(favorite.CountryCode, Country, StringComparison.OrdinalIgnoreCase);

    private bool MatchesSearch(FavoriteCityModel favorite) =>
        Search.Length == 0
        || $"{favorite.City} {favorite.CountryCode} {favorite.Id}".Contains(Search, StringComparison.OrdinalIgnoreCase);
}
