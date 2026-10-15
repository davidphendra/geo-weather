using Weather.Modules.Journal.Domain.Cities;

namespace Weather.Modules.Journal.Domain.FavoriteCities;

/// <summary>A saved city, keyed on CityId (unique per city). Global — no user (ADR-0010 F5).</summary>
public sealed class FavoriteCity
{
    public FavoriteCityId Id { get; }

    public CityId CityId { get; }

    public DateTimeOffset CreatedAtUtc { get; }

    private FavoriteCity(FavoriteCityId id, CityId cityId, DateTimeOffset createdAtUtc)
    {
        Id = id;
        CityId = cityId;
        CreatedAtUtc = createdAtUtc;
    }

    public static FavoriteCity Create(FavoriteCityId id, CityId cityId, DateTimeOffset createdAtUtc)
    {
        ArgumentNullException.ThrowIfNull(id);
        ArgumentNullException.ThrowIfNull(cityId);

        return new FavoriteCity(id, cityId, createdAtUtc);
    }
}
