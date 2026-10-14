using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Weather.Modules.Geography.Infrastructure.DesignTime;

/// <summary>Enables <c>dotnet ef migrations</c> against the Geography context from the class library.</summary>
public sealed class GeographyContextFactory : IDesignTimeDbContextFactory<GeographyContext>
{
    public GeographyContext CreateDbContext(string[] args)
    {
        DbContextOptionsBuilder<GeographyContext> options = new();

        options.UseSqlite("Data Source=geography.db");

        return new GeographyContext(options.Options);
    }
}
