using System.Net;
using System.Text;

using Weather.WebUI.Services;
using Weather.WebUI.UnitTests.Support;

namespace Weather.WebUI.UnitTests.Services;

/// <summary>The Favorite ⇄ Note intent mapping and validation of the dispatch form (e06s03).</summary>
[Trait("Category", "CommandDispatch")]
public sealed class CommandDispatchModelTests
{
    private static readonly CityModel London = new("c1", "London", "GB", 51.5m, -0.1m);

    private static (CommandDispatchModel Model, List<string> Requests) Create(
        Func<HttpRequestMessage, HttpResponseMessage> respond)
    {
        var requests = new List<string>();
        var api = new WeatherApiClient(
            new HttpClient(new StubHttpMessageHandler(request =>
            {
                requests.Add(request.RequestUri?.AbsolutePath ?? string.Empty);
                return respond(request);
            })),
            new TestNavigationManager());

        return (new CommandDispatchModel(api), requests);
    }

    private static HttpResponseMessage Created(string json) =>
        new(HttpStatusCode.Created) { Content = new StringContent(json, Encoding.UTF8, "application/json") };

    [Fact]
    public async Task Favorite_intent_posts_to_the_favorites_endpoint()
    {
        (CommandDispatchModel model, List<string> requests) = Create(_ => Created(
            """{"id":"11111111-1111-1111-1111-111111111111","cityId":"22222222-2222-2222-2222-222222222222","city":"London","countryCode":"GB","createdAtUtc":"2025-01-01T00:00:00+00:00"}"""));

        model.Bind(London, null);
        model.Text = "Primary monitoring node";
        model.Intent = MutationIntent.Favorite;

        await model.SubmitAsync(CancellationToken.None);

        Assert.Equal("/api/cities/favorites", Assert.Single(requests));
        Assert.True(model.LastResult?.IsSuccess);
        Assert.Equal(201, model.LastResult?.Status);
        Assert.Null(model.ValidationError);
    }

    [Fact]
    public async Task Note_intent_posts_to_the_notes_endpoint()
    {
        (CommandDispatchModel model, List<string> requests) = Create(_ => Created(
            """{"id":"11111111-1111-1111-1111-111111111111","cityName":"London","countryCode":"GB","note":"Cold front","temperatureCelsius":10,"createdAtUtc":"2025-01-01T00:00:00+00:00"}"""));

        model.Bind(London, 10m);
        model.Text = "Cold front";
        model.Intent = MutationIntent.Note;

        await model.SubmitAsync(CancellationToken.None);

        Assert.Equal("/api/weather/notes", Assert.Single(requests));
        Assert.True(model.LastResult?.IsSuccess);
    }

    [Fact]
    public async Task Manual_target_posts_to_the_favorites_endpoint()
    {
        (CommandDispatchModel model, List<string> requests) = Create(_ => Created(
            """{"id":"11111111-1111-1111-1111-111111111111","cityId":"22222222-2222-2222-2222-222222222222","city":"Kyoto","countryCode":"JP","createdAtUtc":"2025-01-01T00:00:00+00:00"}"""));

        model.BindManual("Kyoto", "JP");
        model.Text = "Pinned node";

        await model.SubmitAsync(CancellationToken.None);

        Assert.Equal("/api/cities/favorites", Assert.Single(requests));
        Assert.Single(model.History);
        Assert.True(model.LastResult?.IsSuccess);
    }

    [Fact]
    public async Task Empty_text_is_rejected_without_a_request()
    {
        (CommandDispatchModel model, List<string> requests) = Create(_ => Created("{}"));

        model.Bind(London, null);
        model.Text = "   ";

        await model.SubmitAsync(CancellationToken.None);

        Assert.Empty(requests);
        Assert.NotNull(model.ValidationError);
        Assert.Null(model.LastResult);
    }

    [Fact]
    public async Task No_selected_city_is_rejected()
    {
        (CommandDispatchModel model, List<string> requests) = Create(_ => Created("{}"));

        model.Text = "Primary monitoring node";

        await model.SubmitAsync(CancellationToken.None);

        Assert.Empty(requests);
        Assert.NotNull(model.ValidationError);
    }
}
