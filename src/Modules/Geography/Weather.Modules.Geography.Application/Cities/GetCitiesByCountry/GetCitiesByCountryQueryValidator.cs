using FluentValidation;

namespace Weather.Modules.Geography.Application.Cities.GetCitiesByCountry;

internal sealed class GetCitiesByCountryQueryValidator : AbstractValidator<GetCitiesByCountryQuery>
{
    public GetCitiesByCountryQueryValidator()
    {
        RuleFor(query => query.CountryCode).NotEmpty().Length(2);
    }
}
