using Weather.Modules.Journal.Domain.Cities;

namespace Weather.Modules.Journal.Application.Abstractions;

public interface ICityProjectionRepository
{
    Task<City?> FindByNameAndCountryAsync(string cityName, string countryCode, CancellationToken cancellationToken);

    Task UpsertAsync(City city, CancellationToken cancellationToken);
}
