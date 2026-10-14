using MediatR;

namespace Weather.Modules.Geography.Application.Cities.GetCitiesByCountry;

public sealed record GetCitiesByCountryQuery(string CountryCode) : IRequest<IReadOnlyList<CityDto>>;
