using NSubstitute;

using Weather.Modules.Favorites.Application.Abstractions;
using Weather.Modules.Favorites.Application.FavoriteCities.AddFavoriteCity;
using Weather.Modules.Favorites.Application.FavoriteCities.GetFavorites;
using Weather.Modules.Favorites.Domain;
using Weather.Modules.Favorites.Domain.Cities;
using Weather.Modules.Favorites.Domain.Countries;
using Weather.Modules.Favorites.Domain.FavoriteCities;

namespace Weather.Modules.Favorites.Tests.UnitTests.Application;

[Trait("Category", "AddFavorite")]
public sealed class AddFavoriteCityCommandHandlerTests
{
    private static City MakeCity() => City.Create(
        CityId.From(Guid.Parse("00000001-0000-0000-0000-000000000000")),
        CountryCode.From("GB"),
        "London",
        GeoCoordinate.From(51.5074m, -0.1278m));

    private static AddFavoriteCityCommandHandler CreateHandler(
        bool cityExists,
        bool favoriteExists,
        out IFavoriteCityRepository favorites)
    {
        ICityProjectionRepository cityProjections = Substitute.For<ICityProjectionRepository>();
        cityProjections.FindByNameAndCountryAsync("London", "GB", Arg.Any<CancellationToken>())
            .Returns(cityExists ? MakeCity() : null);

        favorites = Substitute.For<IFavoriteCityRepository>();
        favorites.ExistsByCityIdAsync(Arg.Any<CityId>(), Arg.Any<CancellationToken>()).Returns(favoriteExists);

        IUnitOfWork unitOfWork = Substitute.For<IUnitOfWork>();
        IDateTimeProvider dateTimeProvider = Substitute.For<IDateTimeProvider>();
        dateTimeProvider.UtcNow.Returns(DateTimeOffset.UnixEpoch);

        return new AddFavoriteCityCommandHandler(cityProjections, favorites, unitOfWork, dateTimeProvider);
    }

    [Fact]
    public async Task PersistsAndReturnsCreatedFavorite()
    {
        AddFavoriteCityCommandHandler handler = CreateHandler(cityExists: true, favoriteExists: false, out IFavoriteCityRepository favorites);

        FavoriteCityDto dto = await handler.Handle(new AddFavoriteCityCommand("London", "GB"), CancellationToken.None);

        Assert.Equal("London", dto.City);
        Assert.Equal("GB", dto.CountryCode);
        await favorites.Received(1).AddAsync(Arg.Any<FavoriteCity>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ThrowsWhenCityUnknown()
    {
        AddFavoriteCityCommandHandler handler = CreateHandler(cityExists: false, favoriteExists: false, out _);

        await Assert.ThrowsAsync<CityNotFoundException>(
            () => handler.Handle(new AddFavoriteCityCommand("London", "GB"), CancellationToken.None));
    }

    [Fact]
    public async Task ThrowsWhenAlreadyFavorited()
    {
        AddFavoriteCityCommandHandler handler = CreateHandler(cityExists: true, favoriteExists: true, out _);

        await Assert.ThrowsAsync<FavoriteAlreadyExistsException>(
            () => handler.Handle(new AddFavoriteCityCommand("London", "GB"), CancellationToken.None));
    }
}
