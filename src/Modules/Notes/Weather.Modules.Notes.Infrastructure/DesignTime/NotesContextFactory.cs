using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Weather.Modules.Notes.Infrastructure.DesignTime;

/// <summary>Enables <c>dotnet ef migrations</c> against the Notes context.</summary>
public sealed class NotesContextFactory : IDesignTimeDbContextFactory<NotesContext>
{
    public NotesContext CreateDbContext(string[] args)
    {
        DbContextOptionsBuilder<NotesContext> options = new();

        string dataDirectory = Path.Combine(Directory.GetCurrentDirectory(), "App_Data");
        Directory.CreateDirectory(dataDirectory);

        options.UseSqlite($"Data Source={Path.Combine(dataDirectory, "notes.db")}");

        return new NotesContext(options.Options);
    }
}
