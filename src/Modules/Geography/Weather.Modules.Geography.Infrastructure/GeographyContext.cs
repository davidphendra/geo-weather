using Microsoft.EntityFrameworkCore;

using Weather.BuildingBlocks.Domain.SharedKernel;
using Weather.Modules.Geography.Domain.Cities;
using Weather.Modules.Geography.Domain.Countries;

namespace Weather.Modules.Geography.Infrastructure;

public sealed class GeographyContext(DbContextOptions<GeographyContext> options) : DbContext(options)
{
    public DbSet<Country> Countries => Set<Country>();

    public DbSet<City> Cities => Set<City>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(GeographyContext).Assembly);
    }
}
