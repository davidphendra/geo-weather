using NSubstitute;

using Weather.Modules.Favorites.Application.Abstractions;
using Weather.Modules.Favorites.Application.FavoriteCities.GetFavorites;

namespace Weather.Modules.Favorites.Tests.UnitTests.Application;

[Trait("Category", "GetFavorites")]
public sealed class GetFavoritesQueryHandlerTests
{
    [Fact]
    public async Task MapsReadModelToDtos()
    {
        IFavoriteCityReadRepository readRepository = Substitute.For<IFavoriteCityReadRepository>();
        readRepository.GetFavoritesAsync(Arg.Any<CancellationToken>())
            .Returns([new FavoriteCityView(Guid.NewGuid(), Guid.NewGuid(), "London", "GB", DateTimeOffset.UnixEpoch)]);
        var handler = new GetFavoritesQueryHandler(readRepository);

        IReadOnlyList<FavoriteCityDto> result = await handler.Handle(new GetFavoritesQuery(), CancellationToken.None);

        Assert.Single(result);
        Assert.Equal("London", result[0].City);
    }
}
