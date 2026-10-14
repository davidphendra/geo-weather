using Weather.BuildingBlocks.Domain.SharedKernel;
using Weather.Modules.Geography.Domain.Countries;

namespace Weather.Modules.Geography.Application.Abstractions;

/// <summary>Thrown when a requested country is not in the catalog.</summary>
public sealed class CountryNotFoundException(CountryCode countryCode)
    : Exception($"Country '{countryCode}' was not found.");
