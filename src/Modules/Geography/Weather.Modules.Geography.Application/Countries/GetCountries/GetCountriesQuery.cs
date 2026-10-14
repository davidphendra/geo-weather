using MediatR;

namespace Weather.Modules.Geography.Application.Countries.GetCountries;

public sealed record GetCountriesQuery(string? SearchTerm = null) : IRequest<IReadOnlyList<CountryDto>>;
