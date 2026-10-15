using FluentValidation;

namespace Weather.Modules.Weather.Application.WeatherReports.GetWeatherForecast;

internal sealed class GetWeatherForecastQueryValidator : AbstractValidator<GetWeatherForecastQuery>
{
    public GetWeatherForecastQueryValidator()
    {
        RuleFor(query => query.CityName).NotEmpty().MaximumLength(100);
        RuleFor(query => query.CountryCode).Length(2).When(query => !string.IsNullOrWhiteSpace(query.CountryCode));
    }
}
