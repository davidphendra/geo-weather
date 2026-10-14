using Microsoft.AspNetCore.Mvc;

using NSubstitute;

using Weather.Modules.Notes.Application.Contracts;
using Weather.Modules.Notes.Application.Notes;
using Weather.WebUI.Api.Modules.Notes;

namespace Weather.WebUI.UnitTests.Controllers;

[Trait("Category", "NotesController")]
public sealed class NotesControllerTests
{
    private static WeatherNoteDto Note() => new(
        Guid.NewGuid(),
        "London",
        "GB",
        "Chilly",
        12.5m,
        DateTimeOffset.UnixEpoch);

    [Fact]
    public async Task AddNote_ReturnsCreatedWithTheResource()
    {
        INotesModule module = Substitute.For<INotesModule>();
        WeatherNoteDto note = Note();
        module.AddNoteAsync("London", "GB", "Chilly", 12.5m, Arg.Any<CancellationToken>()).Returns(note);
        var controller = new NotesController(module);

        ActionResult<WeatherNoteDto> result = await controller.AddNoteAsync(
            new AddNoteRequest("London", "GB", "Chilly", 12.5m),
            CancellationToken.None);

        CreatedResult created = Assert.IsType<CreatedResult>(result.Result);
        Assert.Equal(note, created.Value);
        Assert.Contains(note.Id.ToString(), created.Location, StringComparison.Ordinal);
    }
}
