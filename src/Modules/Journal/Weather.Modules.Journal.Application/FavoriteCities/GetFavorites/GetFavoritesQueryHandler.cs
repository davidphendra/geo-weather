using System.Diagnostics;
using System.Diagnostics.Metrics;

using MediatR;

using Weather.BuildingBlocks.Application.Telemetry;
using Weather.Modules.Journal.Application.Abstractions;

namespace Weather.Modules.Journal.Application.FavoriteCities.GetFavorites;

internal sealed class GetFavoritesQueryHandler(IFavoriteCityReadRepository readRepository)
    : IRequestHandler<GetFavoritesQuery, IReadOnlyList<FavoriteCityDto>>
{
    public async Task<IReadOnlyList<FavoriteCityDto>> Handle(
        GetFavoritesQuery request,
        CancellationToken cancellationToken)
    {
        long start = Stopwatch.GetTimestamp();
        IReadOnlyList<FavoriteCityView> views = await readRepository.GetFavoritesAsync(cancellationToken);

        TagList tags = new()
        {
            { WeatherMeters.Tags.Operation, "list" },
            { WeatherMeters.Tags.FavoritesOutcome, WeatherMeters.Outcomes.Success },
        };

        WeatherMeters.FavoritesOperations.Add(1, tags);
        WeatherMeters.FavoritesDuration.Record(Stopwatch.GetElapsedTime(start).TotalMilliseconds, tags);

        return [.. views.Select(view => view.ToDto())];
    }
}
