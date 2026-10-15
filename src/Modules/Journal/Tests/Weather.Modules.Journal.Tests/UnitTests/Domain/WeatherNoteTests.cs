using Weather.BuildingBlocks.Domain.SharedKernel;
using Weather.Modules.Journal.Domain;
using Weather.Modules.Journal.Domain.Notes;

namespace Weather.Modules.Journal.Tests.UnitTests.Domain;

[Trait("Category", "WeatherNote")]
public sealed class WeatherNoteTests
{
    [Fact]
    public void Create_StoresFields()
    {
        WeatherNoteId id = WeatherNoteId.New();
        CountryCode code = CountryCode.From("gb");
        DateTimeOffset createdAt = DateTimeOffset.UnixEpoch;

        WeatherNote note = WeatherNote.Create(id, "London", code, "Chilly", 12.5m, createdAt);

        Assert.Equal(id, note.Id);
        Assert.Equal("London", note.CityName);
        Assert.Equal("GB", note.CountryCode.Value);
        Assert.Equal("Chilly", note.Note);
        Assert.Equal(12.5m, note.TemperatureCelsius);
        Assert.Equal(createdAt, note.CreatedAtUtc);
    }

    [Fact]
    public void Create_AllowsNullTemperature()
    {
        WeatherNote note = WeatherNote.Create(
            WeatherNoteId.New(),
            "London",
            CountryCode.From("GB"),
            "Chilly",
            null,
            DateTimeOffset.UnixEpoch);

        Assert.Null(note.TemperatureCelsius);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_RejectsBlankNote(string note)
    {
        Assert.ThrowsAny<ArgumentException>(() => WeatherNote.Create(
            WeatherNoteId.New(),
            "London",
            CountryCode.From("GB"),
            note,
            12.5m,
            DateTimeOffset.UnixEpoch));
    }

    [Fact]
    public void Create_RejectsOversizedNote()
    {
        string oversized = new('x', WeatherNote.MaxNoteLength + 1);

        Assert.Throws<JournalDomainException>(() => WeatherNote.Create(
            WeatherNoteId.New(),
            "London",
            CountryCode.From("GB"),
            oversized,
            null,
            DateTimeOffset.UnixEpoch));
    }

    [Fact]
    public void WeatherNoteId_FromEmpty_Throws()
    {
        Assert.Throws<JournalDomainException>(() => WeatherNoteId.From(Guid.Empty));
    }
}
