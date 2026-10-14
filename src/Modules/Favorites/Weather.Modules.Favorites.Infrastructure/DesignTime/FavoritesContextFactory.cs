using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Weather.Modules.Favorites.Infrastructure.DesignTime;

/// <summary>Enables <c>dotnet ef migrations</c> against the Favorites context.</summary>
public sealed class FavoritesContextFactory : IDesignTimeDbContextFactory<FavoritesContext>
{
    public FavoritesContext CreateDbContext(string[] args)
    {
        DbContextOptionsBuilder<FavoritesContext> options = new();

        options.UseSqlite("Data Source=favorites.db");

        return new FavoritesContext(options.Options);
    }
}
