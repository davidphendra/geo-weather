using MediatR;

using NSubstitute;

using Weather.Modules.Notes.Application.Notes;
using Weather.Modules.Notes.Application.Notes.AddWeatherNote;
using Weather.Modules.Notes.Infrastructure;

namespace Weather.Modules.Notes.Tests.UnitTests.Application;

[Trait("Category", "NotesModule")]
public sealed class NotesModuleTests
{
    [Fact]
    public async Task AddNoteAsync_SendsCommandAndReturnsTheDto()
    {
        ISender sender = Substitute.For<ISender>();
        var expected = new WeatherNoteDto(
            Guid.NewGuid(),
            "London",
            "GB",
            "Chilly",
            12.5m,
            DateTimeOffset.UnixEpoch);

        sender.Send<WeatherNoteDto>(Arg.Any<AddWeatherNoteCommand>(), Arg.Any<CancellationToken>())
            .Returns(expected);

        var module = new NotesModule(sender);

        WeatherNoteDto dto = await module.AddNoteAsync("London", "GB", "Chilly", 12.5m, CancellationToken.None);

        Assert.Equal(expected, dto);
        await sender.Received(1).Send<WeatherNoteDto>(
            Arg.Is<AddWeatherNoteCommand>(command => command.CityName == "London" && command.Note == "Chilly"),
            Arg.Any<CancellationToken>());
    }
}
