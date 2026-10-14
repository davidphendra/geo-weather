using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

using Weather.Modules.Geography.Application.Countries.GetCountries;
using Weather.Modules.Geography.Domain.Countries;
using Weather.Modules.Geography.Infrastructure;
using Weather.Modules.Geography.Infrastructure.Repositories;

namespace Weather.Modules.Geography.Tests.IntegrationTests;

[Trait("Category", "GetCountries")]
public sealed class CountryReadRepositoryTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly GeographyContext _context;
    private readonly CountryReadRepository _repository;

    public CountryReadRepositoryTests()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();

        DbContextOptions<GeographyContext> options = new DbContextOptionsBuilder<GeographyContext>()
            .UseSqlite(_connection)
            .Options;

        _context = new GeographyContext(options);
        _context.Database.Migrate();
        _repository = new CountryReadRepository(_context);
    }

    [Fact]
    public async Task Search_FiltersInTheDatabase()
    {
        IReadOnlyList<Country> matches = await _repository.GetAsync(new CountryQuery("united"), CancellationToken.None);

        Assert.Contains(matches, country => country.Code == CountryCode.From("GB"));
        Assert.DoesNotContain(matches, country => country.Code == CountryCode.From("JP"));
    }

    [Fact]
    public async Task Search_WithNoMatches_ReturnsEmpty()
    {
        IReadOnlyList<Country> matches = await _repository.GetAsync(new CountryQuery("zzzzz"), CancellationToken.None);

        Assert.Empty(matches);
    }

    [Fact]
    public async Task NoFilter_ReturnsTheCatalog_WithoutTracking()
    {
        IReadOnlyList<Country> countries = await _repository.GetAsync(new CountryQuery(), CancellationToken.None);

        Assert.NotEmpty(countries);
        Assert.Empty(_context.ChangeTracker.Entries());
    }

    [Fact]
    public async Task Exists_ReflectsTheCatalog()
    {
        Assert.True(await _repository.ExistsAsync(CountryCode.From("GB"), CancellationToken.None));
        Assert.False(await _repository.ExistsAsync(CountryCode.From("ZZ"), CancellationToken.None));
    }

    public void Dispose()
    {
        _context.Dispose();
        _connection.Dispose();
    }
}
