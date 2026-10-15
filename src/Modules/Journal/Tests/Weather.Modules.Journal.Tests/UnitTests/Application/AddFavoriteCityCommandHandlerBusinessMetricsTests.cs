using System.Diagnostics.Metrics;

using NSubstitute;

using Weather.BuildingBlocks.Application.Abstractions;
using Weather.BuildingBlocks.Application.Telemetry;
using Weather.BuildingBlocks.Domain.SharedKernel;
using Weather.Modules.Journal.Application.Abstractions;
using Weather.Modules.Journal.Application.FavoriteCities.AddFavoriteCity;
using Weather.Modules.Journal.Domain.Cities;
using Weather.Modules.Journal.Domain.FavoriteCities;

namespace Weather.Modules.Journal.Tests.UnitTests.Application;

[Trait("Category", "Observability")]
public sealed class AddFavoriteCityCommandHandlerBusinessMetricsTests
{
    [Fact]
    public async Task Created_RecordsTheCreatedOutcome()
    {
        List<string> outcomes = [];
        using MeterListener listener = OutcomeListener(outcomes);

        await CreateHandler(cityExists: true, favoriteExists: false)
            .Handle(new AddFavoriteCityCommand("London", "GB"), CancellationToken.None);

        Assert.Equal(["created"], outcomes);
    }

    [Fact]
    public async Task Duplicate_RecordsTheDuplicateOutcome()
    {
        List<string> outcomes = [];
        using MeterListener listener = OutcomeListener(outcomes);

        await Assert.ThrowsAsync<FavoriteAlreadyExistsException>(() =>
            CreateHandler(cityExists: true, favoriteExists: true)
                .Handle(new AddFavoriteCityCommand("London", "GB"), CancellationToken.None));

        Assert.Equal(["duplicate"], outcomes);
    }

    [Fact]
    public async Task UnknownCity_RecordsTheCityNotFoundOutcome()
    {
        List<string> outcomes = [];
        using MeterListener listener = OutcomeListener(outcomes);

        await Assert.ThrowsAsync<CityNotFoundException>(() =>
            CreateHandler(cityExists: false, favoriteExists: false)
                .Handle(new AddFavoriteCityCommand("London", "GB"), CancellationToken.None));

        Assert.Equal(["city_not_found"], outcomes);
    }

    private static MeterListener OutcomeListener(List<string> outcomes)
    {
        MeterListener listener = new();
        listener.SetMeasurementEventCallback<long>((instrument, _, tags, _) =>
        {
            if (instrument.Name != "favorites.operations")
            {
                return;
            }

            foreach (KeyValuePair<string, object?> tag in tags)
            {
                if (tag.Key == WeatherMeters.Tags.FavoritesOutcome)
                {
                    outcomes.Add(tag.Value as string ?? string.Empty);
                }
            }
        });
        listener.EnableMeasurementEvents(WeatherMeters.FavoritesOperations);
        listener.Start();

        return listener;
    }

    private static AddFavoriteCityCommandHandler CreateHandler(bool cityExists, bool favoriteExists)
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

        IFavoriteCityRepository favorites = Substitute.For<IFavoriteCityRepository>();
        favorites.ExistsByCityIdAsync(Arg.Any<CityId>(), Arg.Any<CancellationToken>()).Returns(favoriteExists);

        IUnitOfWork unitOfWork = Substitute.For<IUnitOfWork>();
        IDateTimeProvider dateTimeProvider = Substitute.For<IDateTimeProvider>();
        dateTimeProvider.UtcNow.Returns(DateTimeOffset.UnixEpoch);

        return new AddFavoriteCityCommandHandler(cityProjections, favorites, unitOfWork, dateTimeProvider);
    }
}
