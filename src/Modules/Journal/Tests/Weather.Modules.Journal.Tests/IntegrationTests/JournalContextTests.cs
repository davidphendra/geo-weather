using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

using Weather.BuildingBlocks.Domain.SharedKernel;
using Weather.Modules.Journal.Domain;
using Weather.Modules.Journal.Domain.Cities;
using Weather.Modules.Journal.Domain.FavoriteCities;
using Weather.Modules.Journal.Domain.Notes;
using Weather.Modules.Journal.Infrastructure;

namespace Weather.Modules.Journal.Tests.IntegrationTests;

[Trait("Category", "FavoritesMigrations")]
public sealed class JournalContextTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly JournalContext _context;

    public JournalContextTests()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();

        DbContextOptions<JournalContext> options = new DbContextOptionsBuilder<JournalContext>()
            .UseSqlite(_connection)
            .Options;

        _context = new JournalContext(options);
        _context.Database.Migrate();
    }

    [Fact]
    public async Task PersistsAFavorite()
    {
        CityId cityId = CityId.From(Guid.NewGuid());
        _context.CityProjections.Add(City.Create(cityId, CountryCode.From("GB"), "London", GeoCoordinate.From(51.5074m, -0.1278m)));
        _context.FavoriteCities.Add(FavoriteCity.Create(FavoriteCityId.New(), cityId, DateTimeOffset.UnixEpoch));
        await _context.SaveChangesAsync(CancellationToken.None);

        Assert.True(await _context.FavoriteCities.AsNoTracking().AnyAsync(favorite => favorite.CityId == cityId));
    }

    [Fact]
    public async Task PersistsAWeatherNote()
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
    public async Task UniqueIndex_PreventsDuplicateFavoritesForTheSameCity()
    {
        CityId cityId = CityId.From(Guid.NewGuid());
        _context.CityProjections.Add(City.Create(cityId, CountryCode.From("GB"), "London", GeoCoordinate.From(51.5074m, -0.1278m)));
        _context.FavoriteCities.Add(FavoriteCity.Create(FavoriteCityId.New(), cityId, DateTimeOffset.UnixEpoch));
        await _context.SaveChangesAsync(CancellationToken.None);

        _context.FavoriteCities.Add(FavoriteCity.Create(FavoriteCityId.New(), cityId, DateTimeOffset.UnixEpoch));

        await Assert.ThrowsAsync<DbUpdateException>(() => _context.SaveChangesAsync(CancellationToken.None));
    }

    public void Dispose()
    {
        _context.Dispose();
        _connection.Dispose();
    }
}
