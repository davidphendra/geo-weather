using Weather.Modules.Favorites.Domain.Cities;

namespace Weather.Modules.Favorites.Application.Abstractions;

public interface ICityProjectionRepository
{
    Task<City?> FindByNameAndCountryAsync(string cityName, string countryCode, CancellationToken cancellationToken);

    Task UpsertAsync(City city, CancellationToken cancellationToken);
}
