using Weather.BuildingBlocks.Domain;

namespace Weather.Modules.Favorites.Domain;

/// <summary>Thrown when a Favorites domain invariant is violated.</summary>
public sealed class FavoritesDomainException : DomainException
{
    public FavoritesDomainException(string message) : base(message)
    {
    }
}
