using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Weather.Modules.Journal.Infrastructure.DesignTime;

/// <summary>Enables <c>dotnet ef migrations</c> against the Favorites context.</summary>
public sealed class JournalContextFactory : IDesignTimeDbContextFactory<JournalContext>
{
    public JournalContext CreateDbContext(string[] args)
    {
        DbContextOptionsBuilder<JournalContext> options = new();

        string dataDirectory = Path.Combine(Directory.GetCurrentDirectory(), "App_Data");
        Directory.CreateDirectory(dataDirectory);

        options.UseSqlite($"Data Source={Path.Combine(dataDirectory, "journal.db")}");

        return new JournalContext(options.Options);
    }
}
