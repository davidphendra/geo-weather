using Microsoft.AspNetCore.Mvc;

using NSubstitute;

using Weather.Modules.Journal.Application.Contracts;
using Weather.Modules.Journal.Application.FavoriteCities.GetFavorites;
using Weather.WebUI.Api.Modules.Journal;

namespace Weather.WebUI.UnitTests.Controllers;

[Trait("Category", "FavoritesController")]
public sealed class FavoritesControllerTests
{
    private static FavoriteCityDto Favorite() => new(
        Guid.NewGuid(),
        Guid.NewGuid(),
        "London",
        "GB",
        DateTimeOffset.UnixEpoch);

    [Fact]
    public async Task AddFavorite_ReturnsCreatedWithTheResource()
    {
        IFavoritesService service = Substitute.For<IFavoritesService>();
        FavoriteCityDto favorite = Favorite();
        service.AddFavoriteAsync("London", "GB", Arg.Any<CancellationToken>()).Returns(favorite);
        var controller = new FavoritesController(service);

        ActionResult<FavoriteCityDto> result = await controller.AddFavoriteAsync(
            new AddFavoriteRequest("London", "GB"),
            CancellationToken.None);

        CreatedResult created = Assert.IsType<CreatedResult>(result.Result);
        Assert.Equal(favorite, created.Value);
        Assert.Contains(favorite.Id.ToString(), created.Location, StringComparison.Ordinal);
    }

    [Fact]
    public async Task GetFavorites_ReturnsOkWithTheList()
    {
        IFavoritesService service = Substitute.For<IFavoritesService>();
        service.GetFavoritesAsync(Arg.Any<CancellationToken>()).Returns([Favorite()]);
        var controller = new FavoritesController(service);

        ActionResult<IReadOnlyList<FavoriteCityDto>> result = await controller.GetFavoritesAsync(CancellationToken.None);

        OkObjectResult ok = Assert.IsType<OkObjectResult>(result.Result);
        IReadOnlyList<FavoriteCityDto> favorites = Assert.IsAssignableFrom<IReadOnlyList<FavoriteCityDto>>(ok.Value);
        Assert.Single(favorites);
    }
}
