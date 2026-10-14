using System.Diagnostics.Metrics;

using NSubstitute;

using Weather.BuildingBlocks.Application.Telemetry;
using Weather.Modules.Geography.Application.Abstractions;
using Weather.Modules.Geography.Application.Cities.GetCitiesByCountry;
using Weather.Modules.Geography.Application.Countries.GetCountries;
using Weather.Modules.Geography.Domain.Cities;
using Weather.Modules.Geography.Domain.Countries;

namespace Weather.Modules.Geography.Tests.UnitTests.Application;

[Trait("Category", "Observability")]
public sealed class GeographyBusinessMetricsTests
{
    [Fact]
    public async Task ListingCountries_RecordsTheRequestWithHasSearch()
    {
        bool? hasSearch = null;

        using MeterListener listener = new();
        listener.SetMeasurementEventCallback<long>((instrument, _, tags, _) =>
        {
            if (instrument.Name != "geography.country.list_requests")
            {
                return;
            }

            foreach (KeyValuePair<string, object?> tag in tags)
            {
                if (tag.Key == WeatherMeters.Tags.HasSearch)
                {
                    hasSearch = tag.Value as bool?;
                }
            }
        });
        listener.EnableMeasurementEvents(WeatherMeters.GeographyCountryListRequests);
        listener.Start();

        ICountryReadRepository countries = Substitute.For<ICountryReadRepository>();
        countries.GetAsync(Arg.Any<CountryQuery>(), Arg.Any<CancellationToken>())
            .Returns((IReadOnlyList<Country>)[]);

        await new GetCountriesQueryHandler(countries)
            .Handle(new GetCountriesQuery("land"), CancellationToken.None);

        Assert.True(hasSearch);
    }

    [Fact]
    public async Task ListingCities_RecordsTheCountryCode()
    {
        string? countryCode = null;

        using MeterListener listener = new();
        listener.SetMeasurementEventCallback<long>((instrument, _, tags, _) =>
        {
            if (instrument.Name != "geography.city.list_requests")
            {
                return;
            }

            foreach (KeyValuePair<string, object?> tag in tags)
            {
                if (tag.Key == WeatherMeters.Tags.CountryCode)
                {
                    countryCode = tag.Value as string;
                }
            }
        });
        listener.EnableMeasurementEvents(WeatherMeters.GeographyCityListRequests);
        listener.Start();

        ICountryReadRepository countries = Substitute.For<ICountryReadRepository>();
        countries.ExistsAsync(Arg.Any<Weather.BuildingBlocks.Domain.SharedKernel.CountryCode>(), Arg.Any<CancellationToken>())
            .Returns(true);

        ICityReadRepository cities = Substitute.For<ICityReadRepository>();
        cities.GetByCountryAsync(Arg.Any<Weather.BuildingBlocks.Domain.SharedKernel.CountryCode>(), Arg.Any<CancellationToken>())
            .Returns((IReadOnlyList<City>)[]);

        await new GetCitiesByCountryQueryHandler(countries, cities)
            .Handle(new GetCitiesByCountryQuery("GB"), CancellationToken.None);

        Assert.Equal("GB", countryCode);
    }
}
