using MediatR;

using NSubstitute;

using Weather.Modules.Journal.Application.Notes;
using Weather.Modules.Journal.Application.Notes.AddWeatherNote;
using Weather.Modules.Journal.Infrastructure;

namespace Weather.Modules.Journal.Tests.UnitTests.Application;

[Trait("Category", "NotesModule")]
public sealed class JournalModuleTests
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

        var module = new JournalModule(sender);

        WeatherNoteDto dto = await module.AddNoteAsync("London", "GB", "Chilly", 12.5m, CancellationToken.None);

        Assert.Equal(expected, dto);
        await sender.Received(1).Send<WeatherNoteDto>(
            Arg.Is<AddWeatherNoteCommand>(command => command.CityName == "London" && command.Note == "Chilly"),
            Arg.Any<CancellationToken>());
    }
}
