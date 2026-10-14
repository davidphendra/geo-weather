using Microsoft.EntityFrameworkCore;

using Weather.Modules.Notes.Application.Abstractions;
using Weather.Modules.Notes.Domain.Cities;
using Weather.Modules.Notes.Domain.Notes;

namespace Weather.Modules.Notes.Infrastructure;

public sealed class NotesContext(DbContextOptions<NotesContext> options) : DbContext(options), IUnitOfWork
{
    public DbSet<WeatherNote> WeatherNotes => Set<WeatherNote>();

    public DbSet<City> CityProjections => Set<City>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(NotesContext).Assembly);
    }
}
