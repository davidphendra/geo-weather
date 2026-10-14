using Weather.Modules.Notes.Domain.Cities;

namespace Weather.Modules.Notes.Application.Abstractions;

public interface ICityProjectionRepository
{
    Task<City?> FindByNameAndCountryAsync(string cityName, string countryCode, CancellationToken cancellationToken);

    Task UpsertAsync(City city, CancellationToken cancellationToken);
}
