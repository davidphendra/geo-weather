using Weather.Modules.Journal.Domain;
using Weather.Modules.Journal.Domain.Cities;
using Weather.Modules.Journal.Domain.FavoriteCities;

namespace Weather.Modules.Journal.Tests.UnitTests.Domain;

[Trait("Category", "FavoriteDomain")]
public sealed class FavoriteCityTests
{
    [Fact]
    public void Create_StoresFields()
    {
        FavoriteCityId id = FavoriteCityId.New();
        CityId cityId = CityId.From(Guid.NewGuid());
        DateTimeOffset createdAt = DateTimeOffset.UnixEpoch;

        FavoriteCity favorite = FavoriteCity.Create(id, cityId, createdAt);

        Assert.Equal(id, favorite.Id);
        Assert.Equal(cityId, favorite.CityId);
        Assert.Equal(createdAt, favorite.CreatedAtUtc);
    }

    [Fact]
    public void FavoriteCityId_FromEmpty_Throws()
    {
        Assert.Throws<JournalDomainException>(() => FavoriteCityId.From(Guid.Empty));
    }

    [Fact]
    public void Create_RejectsNullCityId()
    {
        Assert.Throws<ArgumentNullException>(
            () => FavoriteCity.Create(FavoriteCityId.New(), null!, DateTimeOffset.UtcNow));
    }
}
