using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Weather.Modules.Geography.Infrastructure.DesignTime;

/// <summary>Enables <c>dotnet ef migrations</c> against the Geography context from the class library.</summary>
public sealed class GeographyContextFactory : IDesignTimeDbContextFactory<GeographyContext>
{
    public GeographyContext CreateDbContext(string[] args)
    {
        DbContextOptionsBuilder<GeographyContext> options = new();

        string dataDirectory = Path.Combine(Directory.GetCurrentDirectory(), "App_Data");
        Directory.CreateDirectory(dataDirectory);

        options.UseSqlite($"Data Source={Path.Combine(dataDirectory, "geography.db")}");

        return new GeographyContext(options.Options);
    }
}
