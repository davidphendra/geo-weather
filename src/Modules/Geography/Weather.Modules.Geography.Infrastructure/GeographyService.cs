using MediatR;

using Weather.Modules.Geography.Application.Cities.GetCitiesByCountry;
using Weather.Modules.Geography.Application.Contracts;
using Weather.Modules.Geography.Application.Countries.GetCountries;

namespace Weather.Modules.Geography.Infrastructure;

internal sealed class GeographyService(ISender sender) : IGeographyService
{
    public Task<IReadOnlyList<CountryDto>> GetCountriesAsync(string? search, CancellationToken cancellationToken)
        => sender.Send(new GetCountriesQuery(search), cancellationToken);

    public Task<IReadOnlyList<CityDto>> GetCitiesAsync(string countryCode, CancellationToken cancellationToken)
        => sender.Send(new GetCitiesByCountryQuery(countryCode), cancellationToken);
}
