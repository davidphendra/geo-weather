using System.Diagnostics;
using System.Diagnostics.Metrics;

using MediatR;

using Weather.BuildingBlocks.Application.Abstractions;
using Weather.BuildingBlocks.Application.Telemetry;
using Weather.Modules.Journal.Application.Abstractions;
using Weather.Modules.Journal.Application.FavoriteCities.GetFavorites;
using Weather.Modules.Journal.Application.Telemetry;
using Weather.Modules.Journal.Domain.Cities;
using Weather.Modules.Journal.Domain.FavoriteCities;

namespace Weather.Modules.Journal.Application.FavoriteCities.AddFavoriteCity;

internal sealed class AddFavoriteCityCommandHandler(
    ICityProjectionRepository cityProjections,
    IFavoriteCityRepository favorites,
    IUnitOfWork unitOfWork,
    IDateTimeProvider dateTimeProvider)
    : IRequestHandler<AddFavoriteCityCommand, FavoriteCityDto>
{
    public async Task<FavoriteCityDto> Handle(AddFavoriteCityCommand request, CancellationToken cancellationToken)
    {
        long start = Stopwatch.GetTimestamp();
        string outcome = WeatherMeters.Outcomes.Error;

        try
        {
            City city = await cityProjections.FindByNameAndCountryAsync(
                request.CityName,
                request.CountryCode,
                cancellationToken)
                ?? throw new CityNotFoundException(request.CityName, request.CountryCode);

            if (await favorites.ExistsByCityIdAsync(city.CityId, cancellationToken))
            {
                throw new FavoriteAlreadyExistsException(city.CityId);
            }

            FavoriteCity favorite = FavoriteCity.Create(FavoriteCityId.New(), city.CityId, dateTimeProvider.UtcNow);

            await favorites.AddAsync(favorite, cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);

            outcome = WeatherMeters.Outcomes.Created;

            return new FavoriteCityDto(
                favorite.Id.Value,
                city.CityId.Value,
                city.Name,
                city.CountryCode.Value,
                favorite.CreatedAtUtc);
        }
        catch (CityNotFoundException)
        {
            outcome = WeatherMeters.Outcomes.CityNotFound;

            throw;
        }
        catch (FavoriteAlreadyExistsException)
        {
            outcome = WeatherMeters.Outcomes.Duplicate;

            throw;
        }
        finally
        {
            TagList tags = new()
            {
                { WeatherMeters.Tags.Operation, "add" },
                { WeatherMeters.Tags.FavoritesOutcome, outcome },
            };

            WeatherMeters.FavoritesOperations.Add(1, tags);
            WeatherMeters.FavoritesDuration.Record(Stopwatch.GetElapsedTime(start).TotalMilliseconds, tags);

            if (outcome == WeatherMeters.Outcomes.Created)
            {
                FavoritesMetrics.Saved();
            }
        }
    }
}
