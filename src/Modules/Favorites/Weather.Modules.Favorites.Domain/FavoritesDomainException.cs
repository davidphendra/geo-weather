namespace Weather.Modules.Favorites.Domain;

/// <summary>Thrown when a Favorites domain invariant is violated.</summary>
public sealed class FavoritesDomainException : Exception
{
    public FavoritesDomainException(string message) : base(message)
    {
    }
}
