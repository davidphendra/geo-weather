using System.Diagnostics.Metrics;

using NSubstitute;

using Weather.BuildingBlocks.Application.Abstractions;
using Weather.BuildingBlocks.Application.Telemetry;
using Weather.BuildingBlocks.Domain.SharedKernel;
using Weather.Modules.Journal.Application.Abstractions;
using Weather.Modules.Journal.Application.Notes;
using Weather.Modules.Journal.Application.Notes.AddWeatherNote;
using Weather.Modules.Journal.Domain.Cities;

namespace Weather.Modules.Journal.Tests.UnitTests.Application;

[Trait("Category", "Observability")]
public sealed class AddWeatherNoteCommandHandlerBusinessMetricsTests
{
    [Fact]
    public async Task Created_RecordsTheCreatedOutcome()
    {
        List<string> outcomes = [];
        using MeterListener listener = OutcomeListener(outcomes);

        await CreateHandler(cityExists: true)
            .Handle(new AddWeatherNoteCommand("London", "GB", "Chilly", 12.5m), CancellationToken.None);

        Assert.Equal(["created"], outcomes);
    }

    [Fact]
    public async Task UnknownCity_RecordsTheCityNotFoundOutcome()
    {
        List<string> outcomes = [];
        using MeterListener listener = OutcomeListener(outcomes);

        await Assert.ThrowsAsync<CityNotFoundException>(() =>
            CreateHandler(cityExists: false)
                .Handle(new AddWeatherNoteCommand("London", "GB", "Chilly", null), CancellationToken.None));

        Assert.Equal(["city_not_found"], outcomes);
    }

    private static MeterListener OutcomeListener(List<string> outcomes)
    {
        MeterListener listener = new();
        listener.SetMeasurementEventCallback<long>((instrument, _, tags, _) =>
        {
            if (instrument.Name != "notes.operations")
            {
                return;
            }

            foreach (KeyValuePair<string, object?> tag in tags)
            {
                if (tag.Key == WeatherMeters.Tags.NotesOutcome)
                {
                    outcomes.Add(tag.Value as string ?? string.Empty);
                }
            }
        });
        listener.EnableMeasurementEvents(WeatherMeters.NotesOperations);
        listener.Start();

        return listener;
    }

    private static AddWeatherNoteCommandHandler CreateHandler(bool cityExists)
    {
        ICityProjectionRepository cityProjections = Substitute.For<ICityProjectionRepository>();
        cityProjections.FindByNameAndCountryAsync("London", "GB", Arg.Any<CancellationToken>())
            .Returns(cityExists
                ? City.Create(
                    CityId.From(Guid.Parse("00000001-0000-0000-0000-000000000000")),
                    CountryCode.From("GB"),
                    "London",
                    GeoCoordinate.From(51.5074m, -0.1278m))
                : null);

        IWeatherNoteRepository notes = Substitute.For<IWeatherNoteRepository>();
        IUnitOfWork unitOfWork = Substitute.For<IUnitOfWork>();
        IDateTimeProvider dateTimeProvider = Substitute.For<IDateTimeProvider>();
        dateTimeProvider.UtcNow.Returns(DateTimeOffset.UnixEpoch);

        return new AddWeatherNoteCommandHandler(cityProjections, notes, unitOfWork, dateTimeProvider);
    }
}
