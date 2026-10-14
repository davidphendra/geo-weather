using Weather.BuildingBlocks.Domain.SharedKernel;
using Weather.Modules.Geography.Domain.Countries;

namespace Weather.Modules.Geography.Application.Countries.GetCountries;

public sealed record CountryDto(string Code, string Name);

internal static class CountryDtoMapper
{
    public static CountryDto ToDto(this Country country)
    {
        ArgumentNullException.ThrowIfNull(country);

        return new CountryDto(country.Code.Value, country.Name);
    }
}
