using MediatR;

using Weather.BuildingBlocks.Domain.SharedKernel;
using Weather.Modules.Geography.Application.Abstractions;
using Weather.Modules.Geography.Domain.Cities;
using Weather.Modules.Geography.Domain.Countries;

namespace Weather.Modules.Geography.Application.Cities.GetCitiesByCountry;

internal sealed class GetCitiesByCountryQueryHandler(
    ICountryReadRepository countryReadRepository,
    ICityReadRepository cityReadRepository)
    : IRequestHandler<GetCitiesByCountryQuery, IReadOnlyList<CityDto>>
{
    public async Task<IReadOnlyList<CityDto>> Handle(
        GetCitiesByCountryQuery request,
        CancellationToken cancellationToken)
    {
        CountryCode countryCode = CountryCode.From(request.CountryCode);

        if (!await countryReadRepository.ExistsAsync(countryCode, cancellationToken))
        {
            throw new CountryNotFoundException(countryCode);
        }

        IReadOnlyList<City> cities = await cityReadRepository.GetByCountryAsync(countryCode, cancellationToken);

        return [.. cities.Select(city => city.ToDto())];
    }
}
