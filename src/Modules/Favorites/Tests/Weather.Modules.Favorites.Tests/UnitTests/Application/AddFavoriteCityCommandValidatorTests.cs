using Weather.Modules.Favorites.Application.FavoriteCities.AddFavoriteCity;

namespace Weather.Modules.Favorites.Tests.UnitTests.Application;

[Trait("Category", "AddFavorite")]
public sealed class AddFavoriteCityCommandValidatorTests
{
    private readonly AddFavoriteCityCommandValidator _validator = new();

    [Fact]
    public void ValidCommand_Passes()
    {
        Assert.True(_validator.Validate(new AddFavoriteCityCommand("London", "GB")).IsValid);
    }

    [Fact]
    public void EmptyCity_Fails()
    {
        Assert.False(_validator.Validate(new AddFavoriteCityCommand(string.Empty, "GB")).IsValid);
    }

    [Fact]
    public void BadCountryCode_Fails()
    {
        Assert.False(_validator.Validate(new AddFavoriteCityCommand("London", "GBR")).IsValid);
    }
}
