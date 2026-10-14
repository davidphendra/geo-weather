using FluentValidation;

namespace Weather.Modules.Favorites.Application.FavoriteCities.AddFavoriteCity;

internal sealed class AddFavoriteCityCommandValidator : AbstractValidator<AddFavoriteCityCommand>
{
    public AddFavoriteCityCommandValidator()
    {
        RuleFor(command => command.CityName).NotEmpty().MaximumLength(100);
        RuleFor(command => command.CountryCode).NotEmpty().Length(2);
    }
}
