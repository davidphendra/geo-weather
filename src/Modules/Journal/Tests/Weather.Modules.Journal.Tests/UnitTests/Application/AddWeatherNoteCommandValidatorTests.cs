using Weather.Modules.Journal.Application.Notes.AddWeatherNote;

namespace Weather.Modules.Journal.Tests.UnitTests.Application;

[Trait("Category", "AddNoteValidator")]
public sealed class AddWeatherNoteCommandValidatorTests
{
    private readonly AddWeatherNoteCommandValidator _validator = new();

    [Fact]
    public void ValidCommand_Passes()
    {
        Assert.True(_validator.Validate(new AddWeatherNoteCommand("London", "GB", "Chilly", 12.5m)).IsValid);
    }

    [Fact]
    public void EmptyNote_Fails()
    {
        Assert.False(_validator.Validate(new AddWeatherNoteCommand("London", "GB", string.Empty, 12.5m)).IsValid);
    }

    [Fact]
    public void OversizedNote_Fails()
    {
        string oversized = new('x', 501);

        Assert.False(_validator.Validate(new AddWeatherNoteCommand("London", "GB", oversized, null)).IsValid);
    }

    [Fact]
    public void BadCountryCode_Fails()
    {
        Assert.False(_validator.Validate(new AddWeatherNoteCommand("London", "GBR", "Chilly", null)).IsValid);
    }
}
