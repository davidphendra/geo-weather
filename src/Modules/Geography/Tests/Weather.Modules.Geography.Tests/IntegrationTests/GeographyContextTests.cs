using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

using Weather.Modules.Geography.Domain.Countries;
using Weather.Modules.Geography.Infrastructure;

namespace Weather.Modules.Geography.Tests.IntegrationTests;

[Trait("Category", "GeographyMigrations")]
public sealed class GeographyContextTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly GeographyContext _context;

    public GeographyContextTests()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();

        DbContextOptions<GeographyContext> options = new DbContextOptionsBuilder<GeographyContext>()
            .UseSqlite(_connection)
            .Options;

        _context = new GeographyContext(options);
        _context.Database.Migrate();
    }

    [Fact]
    public async Task Migrations_SeedTheFullCatalog()
    {
        List<Country> countries = await _context.Countries.AsNoTracking().ToListAsync();

        Assert.True(countries.Count >= 200);
        Assert.Contains(countries, country => country.Code == CountryCode.From("GB"));
        Assert.Contains(countries, country => country.Code == CountryCode.From("US"));
    }

    [Fact]
    public void SeededCountry_HasExpectedName()
    {
        Country? germany = _context.Countries.SingleOrDefault(country => country.Code == CountryCode.From("DE"));

        Assert.NotNull(germany);
        Assert.Contains("Germany", germany!.Name);
    }

    public void Dispose()
    {
        _context.Dispose();
        _connection.Dispose();
    }
}
