using System.Diagnostics;
using System.Diagnostics.Metrics;

using MediatR;

using Weather.BuildingBlocks.Application.Telemetry;
using Weather.BuildingBlocks.Domain.SharedKernel;
using Weather.Modules.Geography.Application.Abstractions;
using Weather.Modules.Geography.Domain.Countries;

namespace Weather.Modules.Geography.Application.Countries.GetCountries;

internal sealed class GetCountriesQueryHandler(ICountryReadRepository countryReadRepository)
    : IRequestHandler<GetCountriesQuery, IReadOnlyList<CountryDto>>
{
    public async Task<IReadOnlyList<CountryDto>> Handle(
        GetCountriesQuery request,
        CancellationToken cancellationToken)
    {
        WeatherMeters.GeographyCountryListRequests.Add(1, new TagList
        {
            { WeatherMeters.Tags.HasSearch, !string.IsNullOrWhiteSpace(request.SearchTerm) },
        });

        IReadOnlyList<Country> countries = await countryReadRepository.GetAsync(
            new CountryQuery(request.SearchTerm),
            cancellationToken);

        return [.. countries.Select(country => country.ToDto())];
    }
}
