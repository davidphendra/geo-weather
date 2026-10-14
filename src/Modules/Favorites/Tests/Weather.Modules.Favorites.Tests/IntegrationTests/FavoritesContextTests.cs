using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

using Weather.Modules.Favorites.Domain;
using Weather.Modules.Favorites.Domain.Cities;
using Weather.Modules.Favorites.Domain.Countries;
using Weather.Modules.Favorites.Domain.FavoriteCities;
using Weather.Modules.Favorites.Infrastructure;

namespace Weather.Modules.Favorites.Tests.IntegrationTests;

[Trait("Category", "FavoritesMigrations")]
public sealed class FavoritesContextTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly FavoritesContext _context;

    public FavoritesContextTests()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();

        DbContextOptions<FavoritesContext> options = new DbContextOptionsBuilder<FavoritesContext>()
            .UseSqlite(_connection)
            .Options;

        _context = new FavoritesContext(options);
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
