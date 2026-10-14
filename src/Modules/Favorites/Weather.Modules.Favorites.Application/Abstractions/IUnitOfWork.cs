namespace Weather.Modules.Favorites.Application.Abstractions;

/// <summary>Commits the Favorites unit of work (its own DbContext).</summary>
public interface IUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}
