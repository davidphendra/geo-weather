using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

using Weather.IntegrationTests.Support;
using Weather.Modules.Notes.Infrastructure;

namespace Weather.IntegrationTests.Http;

[Trait("Category", "Notes")]
public sealed class NotesErrorsTests(WeatherHostFactory factory) : IClassFixture<WeatherHostFactory>
{
    [Fact]
    public async Task UnknownCity_Returns404()
    {
        HttpResponseMessage response = await factory.CreateClient()
            .PostAsJsonAsync(
                "/api/weather/notes",
                new { cityName = "Nowhere", countryCode = "ZZ", note = "Chilly", temperatureCelsius = 12.5 });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

        using JsonDocument document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal("RESOURCE_NOT_FOUND", document.RootElement.GetProperty("code").GetString());
    }

    [Fact]
    [Trait("Category", "NotesValidation")]
    public async Task EmptyNote_Returns422AndWritesNoRow()
    {
        int before = await CountNotesAsync();

        HttpResponseMessage response = await factory.CreateClient()
            .PostAsJsonAsync("/api/weather/notes", new { cityName = "London", countryCode = "GB", note = string.Empty });

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);

        using JsonDocument document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal("VALIDATION_FAILED", document.RootElement.GetProperty("code").GetString());
        Assert.NotEmpty(document.RootElement.GetProperty("errors").EnumerateArray());

        Assert.Equal(before, await CountNotesAsync());
    }

    private async Task<int> CountNotesAsync()
    {
        using IServiceScope scope = factory.Services.CreateScope();
        NotesContext context = scope.ServiceProvider.GetRequiredService<NotesContext>();

        return await context.WeatherNotes.AsNoTracking().CountAsync();
    }
}
