using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

using Weather.BuildingBlocks.Domain.SharedKernel;
using Weather.Modules.Notes.Domain.Cities;
using Weather.Modules.Notes.Domain.Notes;
using Weather.Modules.Notes.Infrastructure;

namespace Weather.Modules.Notes.Tests.IntegrationTests;

[Trait("Category", "NotesContext")]
public sealed class NotesContextTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly NotesContext _context;

    public NotesContextTests()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();

        DbContextOptions<NotesContext> options = new DbContextOptionsBuilder<NotesContext>()
            .UseSqlite(_connection)
            .Options;

        _context = new NotesContext(options);
        _context.Database.Migrate();
    }

    [Fact]
    public async Task PersistsANote()
    {
        WeatherNote note = WeatherNote.Create(
            WeatherNoteId.New(),
            "London",
            CountryCode.From("GB"),
            "Chilly",
            12.5m,
            DateTimeOffset.UnixEpoch);

        _context.WeatherNotes.Add(note);
        await _context.SaveChangesAsync(CancellationToken.None);

        Assert.True(await _context.WeatherNotes.AsNoTracking().AnyAsync(existing => existing.Id == note.Id));
    }

    [Fact]
    public async Task PersistsACityProjection()
    {
        CityId cityId = CityId.From(Guid.NewGuid());
        _context.CityProjections.Add(City.Create(cityId, CountryCode.From("GB"), "London"));
        await _context.SaveChangesAsync(CancellationToken.None);

        Assert.True(await _context.CityProjections.AsNoTracking().AnyAsync(city => city.CityId == cityId));
    }

    public void Dispose()
    {
        _context.Dispose();
        _connection.Dispose();
    }
}
