using global::Weather.Modules.Geography.IntegrationEvents;

using NSubstitute;

using Weather.Modules.Journal.Application.Abstractions;
using Weather.Modules.Journal.Application.Cities;
using Weather.Modules.Journal.Domain.Cities;

namespace Weather.Modules.Journal.Tests.UnitTests.Application;

[Trait("Category", "CityAddedHandler")]
public sealed class CityAddedIntegrationEventHandlerTests
{
    [Fact]
    public async Task UpsertsTheCityProjection()
    {
        ICityProjectionRepository cityProjections = Substitute.For<ICityProjectionRepository>();
        IUnitOfWork unitOfWork = Substitute.For<IUnitOfWork>();
        var handler = new CityAddedIntegrationEventHandler(cityProjections, unitOfWork);

        var integrationEvent = new CityAddedIntegrationEvent(
            Guid.Parse("00000001-0000-0000-0000-000000000000"),
            "GB",
            "London",
            51.5074m,
            -0.1278m);

        await handler.HandleAsync(integrationEvent, CancellationToken.None);

        await cityProjections.Received(1).UpsertAsync(
            Arg.Is<City>(city => city.Name == "London" && city.CountryCode.Value == "GB"),
            Arg.Any<CancellationToken>());
        await unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
