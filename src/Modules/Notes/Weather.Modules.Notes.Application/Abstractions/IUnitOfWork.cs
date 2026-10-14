namespace Weather.Modules.Notes.Application.Abstractions;

/// <summary>Commits the Notes unit of work (its own DbContext).</summary>
public interface IUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}
