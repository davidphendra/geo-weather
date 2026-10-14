namespace Weather.Modules.Favorites.Domain.FavoriteCities;

/// <summary>Identity of a favorite.</summary>
public sealed record FavoriteCityId
{
    public Guid Value { get; }

    private FavoriteCityId(Guid value) => Value = value;

    public static FavoriteCityId New() => new(Guid.NewGuid());

    public static FavoriteCityId From(Guid value) => value == Guid.Empty
        ? throw new FavoritesDomainException("Favorite id cannot be empty.")
        : new FavoriteCityId(value);
}
