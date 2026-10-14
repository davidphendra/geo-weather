using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

using Weather.BuildingBlocks.Domain.SharedKernel;
using Weather.Modules.Geography.Domain.Cities;
using Weather.Modules.Geography.Domain.Countries;
using Weather.Modules.Geography.Infrastructure;
using Weather.Modules.Geography.Infrastructure.Repositories;

namespace Weather.Modules.Geography.Tests.IntegrationTests;

[Trait("Category", "GetCities")]
public sealed class CityReadRepositoryTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly GeographyContext _context;
    private readonly CityReadRepository _repository;

    public CityReadRepositoryTests()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();

        DbContextOptions<GeographyContext> options = new DbContextOptionsBuilder<GeographyContext>()
            .UseSqlite(_connection)
            .Options;

        _context = new GeographyContext(options);
        _context.Database.Migrate();
        _repository = new CityReadRepository(_context);
    }

    [Fact]
    public async Task ReturnsSeededCitiesForCountry_WithoutTracking()
    {
        IReadOnlyList<City> cities = await _repository.GetByCountryAsync(CountryCode.From("GB"), CancellationToken.None);

        Assert.NotEmpty(cities);
        Assert.All(cities, city => Assert.Equal("GB", city.CountryCode.Value));
        Assert.Empty(_context.ChangeTracker.Entries());
    }

    [Fact]
    public async Task UnknownCountry_ReturnsEmpty()
    {
        IReadOnlyList<City> cities = await _repository.GetByCountryAsync(CountryCode.From("ZZ"), CancellationToken.None);

        Assert.Empty(cities);
    }

    public void Dispose()
    {
        _context.Dispose();
        _connection.Dispose();
    }
}
