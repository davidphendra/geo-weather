using Weather.WebUI.Services;

namespace Weather.WebUI.UnitTests.Services;

/// <summary>Search/country filtering of the Favorites entity table (e06s04).</summary>
[Trait("Category", "EntityFilter")]
public sealed class EntityFilterModelTests
{
    private static readonly FavoriteCityModel London = new(Guid.NewGuid(), Guid.NewGuid(), "London", "GB", DateTimeOffset.UnixEpoch);
    private static readonly FavoriteCityModel Tokyo = new(Guid.NewGuid(), Guid.NewGuid(), "Tokyo", "JP", DateTimeOffset.UnixEpoch);
    private static readonly FavoriteCityModel Manchester = new(Guid.NewGuid(), Guid.NewGuid(), "Manchester", "GB", DateTimeOffset.UnixEpoch);

    private static EntityFilterModel Model()
    {
        var model = new EntityFilterModel();
        model.Set([London, Tokyo, Manchester]);
        return model;
    }

    [Fact]
    public void Shows_everything_by_default() => Assert.Equal(3, Model().Visible.Count);

    [Fact]
    public void Filters_by_country()
    {
        EntityFilterModel model = Model();
        model.Country = "GB";

        Assert.Equal(2, model.Visible.Count);
        Assert.All(model.Visible, favorite => Assert.Equal("GB", favorite.CountryCode));
    }

    [Fact]
    public void Filters_by_search()
    {
        EntityFilterModel model = Model();
        model.Search = "tok";

        Assert.Equal("Tokyo", Assert.Single(model.Visible).City);
    }

    [Fact]
    public void Exposes_distinct_countries()
    {
        string[] countries = Model().Countries.OrderBy(value => value).ToArray();

        Assert.Equal(new[] { "GB", "JP" }, countries);
    }

    [Fact]
    public void An_empty_catalog_yields_no_rows() => Assert.Empty(new EntityFilterModel().Visible);
}
