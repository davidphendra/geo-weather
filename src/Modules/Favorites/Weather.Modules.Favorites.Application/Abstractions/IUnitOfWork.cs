namespace Weather.Modules.Favorites.Application.Abstractions;

public interface IUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}

public interface IDateTimeProvider
{
    DateTimeOffset UtcNow { get; }
}
