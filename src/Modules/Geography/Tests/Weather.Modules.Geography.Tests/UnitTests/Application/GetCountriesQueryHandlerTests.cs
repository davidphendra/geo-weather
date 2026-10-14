using NSubstitute;

using Weather.Modules.Geography.Application.Abstractions;
using Weather.Modules.Geography.Application.Countries.GetCountries;
using Weather.Modules.Geography.Domain.Countries;

namespace Weather.Modules.Geography.Tests.UnitTests.Application;

[Trait("Category", "GetCountries")]
public sealed class GetCountriesQueryHandlerTests
{
    private static Country MakeCountry(string code, string name) => Country.Create(CountryCode.From(code), name);

    [Fact]
    public async Task ReturnsMappedDtos()
    {
        ICountryReadRepository repository = Substitute.For<ICountryReadRepository>();
        repository.GetAsync(Arg.Any<CountryQuery>(), Arg.Any<CancellationToken>())
            .Returns([MakeCountry("GB", "United Kingdom"), MakeCountry("US", "United States")]);

        var handler = new GetCountriesQueryHandler(repository);

        IReadOnlyList<CountryDto> result = await handler.Handle(new GetCountriesQuery(), CancellationToken.None);

        Assert.Equal(2, result.Count);
        Assert.Contains(result, dto => dto.Code == "GB");
    }

    [Fact]
    public async Task PassesSearchTermToRepository()
    {
        ICountryReadRepository repository = Substitute.For<ICountryReadRepository>();
        repository.GetAsync(Arg.Any<CountryQuery>(), Arg.Any<CancellationToken>()).Returns([]);

        var handler = new GetCountriesQueryHandler(repository);

        await handler.Handle(new GetCountriesQuery("united"), CancellationToken.None);

        await repository.Received(1).GetAsync(
            Arg.Is<CountryQuery>(query => query.SearchTerm == "united"),
            Arg.Any<CancellationToken>());
    }
}
