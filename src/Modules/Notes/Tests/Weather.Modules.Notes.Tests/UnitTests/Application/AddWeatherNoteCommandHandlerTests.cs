using NSubstitute;

using Weather.BuildingBlocks.Application.Abstractions;
using Weather.BuildingBlocks.Domain.SharedKernel;
using Weather.Modules.Notes.Application.Abstractions;
using Weather.Modules.Notes.Application.Notes;
using Weather.Modules.Notes.Application.Notes.AddWeatherNote;
using Weather.Modules.Notes.Domain.Cities;
using Weather.Modules.Notes.Domain.Notes;

namespace Weather.Modules.Notes.Tests.UnitTests.Application;

[Trait("Category", "AddNoteHandler")]
public sealed class AddWeatherNoteCommandHandlerTests
{
    private static City MakeCity() => City.Create(
        CityId.From(Guid.Parse("00000001-0000-0000-0000-000000000000")),
        CountryCode.From("GB"),
        "London");

    private static AddWeatherNoteCommandHandler CreateHandler(bool cityExists, out IWeatherNoteRepository notes)
    {
        ICityProjectionRepository cityProjections = Substitute.For<ICityProjectionRepository>();
        cityProjections.FindByNameAndCountryAsync("London", "GB", Arg.Any<CancellationToken>())
            .Returns(cityExists ? MakeCity() : null);

        notes = Substitute.For<IWeatherNoteRepository>();

        IUnitOfWork unitOfWork = Substitute.For<IUnitOfWork>();
        IDateTimeProvider dateTimeProvider = Substitute.For<IDateTimeProvider>();
        dateTimeProvider.UtcNow.Returns(DateTimeOffset.UnixEpoch);

        return new AddWeatherNoteCommandHandler(cityProjections, notes, unitOfWork, dateTimeProvider);
    }

    [Fact]
    public async Task PersistsAndReturnsTheCreatedNote()
    {
        AddWeatherNoteCommandHandler handler = CreateHandler(cityExists: true, out IWeatherNoteRepository notes);

        WeatherNoteDto dto = await handler.Handle(
            new AddWeatherNoteCommand("London", "GB", "Chilly", 12.5m),
            CancellationToken.None);

        Assert.Equal("London", dto.CityName);
        Assert.Equal("GB", dto.CountryCode);
        Assert.Equal("Chilly", dto.Note);
        Assert.Equal(12.5m, dto.TemperatureCelsius);
        Assert.Equal(DateTimeOffset.UnixEpoch, dto.CreatedAtUtc);
        await notes.Received(1).AddAsync(Arg.Any<WeatherNote>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    [Trait("Category", "UnknownCity")]
    public async Task ThrowsWhenCityUnknown()
    {
        AddWeatherNoteCommandHandler handler = CreateHandler(cityExists: false, out _);

        await Assert.ThrowsAsync<CityNotFoundException>(
            () => handler.Handle(new AddWeatherNoteCommand("London", "GB", "Chilly", null), CancellationToken.None));
    }
}
