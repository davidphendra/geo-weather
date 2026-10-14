using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Weather.Modules.Favorites.Infrastructure.DesignTime;

/// <summary>Enables <c>dotnet ef migrations</c> against the Favorites context.</summary>
public sealed class FavoritesContextFactory : IDesignTimeDbContextFactory<FavoritesContext>
{
    public FavoritesContext CreateDbContext(string[] args)
    {
        DbContextOptionsBuilder<FavoritesContext> options = new();

        string dataDirectory = Path.Combine(Directory.GetCurrentDirectory(), "App_Data");
        Directory.CreateDirectory(dataDirectory);

        options.UseSqlite($"Data Source={Path.Combine(dataDirectory, "favorites.db")}");

        return new FavoritesContext(options.Options);
    }
}
