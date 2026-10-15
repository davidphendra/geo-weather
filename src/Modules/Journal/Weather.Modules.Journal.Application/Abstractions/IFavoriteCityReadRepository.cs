namespace Weather.Modules.Journal.Application.Abstractions;

/// <summary>Read model produced by joining FavoriteCities to CityProjections (in SQL).</summary>
public sealed record FavoriteCityView(
    Guid Id,
    Guid CityId,
    string CityName,
    string CountryCode,
    DateTimeOffset CreatedAtUtc);

public interface IFavoriteCityReadRepository
{
    /// <summary>Returns favorites joined to the city projection. The join runs in the database.</summary>
    Task<IReadOnlyList<FavoriteCityView>> GetFavoritesAsync(CancellationToken cancellationToken);
}
