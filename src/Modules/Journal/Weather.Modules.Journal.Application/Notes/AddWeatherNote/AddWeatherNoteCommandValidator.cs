using FluentValidation;

namespace Weather.Modules.Journal.Application.Notes.AddWeatherNote;

internal sealed class AddWeatherNoteCommandValidator : AbstractValidator<AddWeatherNoteCommand>
{
    public AddWeatherNoteCommandValidator()
    {
        RuleFor(command => command.CityName).NotEmpty().MaximumLength(100);
        RuleFor(command => command.CountryCode).NotEmpty().Length(2);
        RuleFor(command => command.Note).NotEmpty().MaximumLength(500);
    }
}
