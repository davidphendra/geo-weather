using FluentValidation;

namespace Weather.Modules.Weather.Application.WeatherReports.GetCurrentWeather;

internal sealed class GetCurrentWeatherQueryValidator : AbstractValidator<GetCurrentWeatherQuery>
{
    public GetCurrentWeatherQueryValidator()
    {
        RuleFor(query => query.CityName).NotEmpty().MaximumLength(100);
        RuleFor(query => query.CountryCode).Length(2).When(query => !string.IsNullOrWhiteSpace(query.CountryCode));
    }
}
