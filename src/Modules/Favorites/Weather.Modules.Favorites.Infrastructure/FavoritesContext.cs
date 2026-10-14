using Microsoft.EntityFrameworkCore;

using Weather.Modules.Favorites.Application.Abstractions;
using Weather.Modules.Favorites.Domain.Cities;
using Weather.Modules.Favorites.Domain.FavoriteCities;

namespace Weather.Modules.Favorites.Infrastructure;

public sealed class FavoritesContext(DbContextOptions<FavoritesContext> options) : DbContext(options), IUnitOfWork
{
    public DbSet<FavoriteCity> FavoriteCities => Set<FavoriteCity>();

    public DbSet<City> CityProjections => Set<City>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(FavoritesContext).Assembly);
    }
}
