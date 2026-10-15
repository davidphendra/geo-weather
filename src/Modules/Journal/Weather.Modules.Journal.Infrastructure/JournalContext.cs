using Microsoft.EntityFrameworkCore;

using Weather.Modules.Journal.Application.Abstractions;
using Weather.Modules.Journal.Domain.Cities;
using Weather.Modules.Journal.Domain.FavoriteCities;
using Weather.Modules.Journal.Domain.Notes;

namespace Weather.Modules.Journal.Infrastructure;

public sealed class JournalContext(DbContextOptions<JournalContext> options) : DbContext(options), IUnitOfWork
{
    public DbSet<FavoriteCity> FavoriteCities => Set<FavoriteCity>();

    public DbSet<WeatherNote> WeatherNotes => Set<WeatherNote>();

    public DbSet<City> CityProjections => Set<City>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(JournalContext).Assembly);
    }
}
