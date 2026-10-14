using MediatR;

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
        IReadOnlyList<Country> countries = await countryReadRepository.GetAsync(
            new CountryQuery(request.SearchTerm),
            cancellationToken);

        return countries.Select(country => country.ToDto()).ToArray();
    }
}
