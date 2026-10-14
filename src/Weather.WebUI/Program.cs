using System.Diagnostics;

using Microsoft.AspNetCore.Mvc;

using Weather.BuildingBlocks.Infrastructure;
using Weather.Modules.Favorites.Infrastructure;
using Weather.Modules.Geography.Infrastructure;
using Weather.Modules.Notes.Infrastructure;
using Weather.Modules.Weather.Infrastructure;
using Weather.WebUI.Api.ErrorHandling;
using Weather.WebUI.Components;
using Weather.WebUI.Configuration;
using Weather.WebUI.Security;
using Weather.WebUI.Services;

var builder = WebApplication.CreateBuilder(args);

// Blazor.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// REST API.
builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddCsp(builder.Configuration);

// Same-origin REST client for the Blazor UI; the origin is resolved per request from
// NavigationManager.BaseUri, so there is deliberately no configured base address (D7).
builder.Services.AddHttpClient<WeatherApiClient>();
builder.Services.AddScoped<WeatherExplorerModel>();
builder.Services.AddScoped<CommandDispatchModel>();
builder.Services.AddScoped<EntityFilterModel>();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails(options => options.CustomizeProblemDetails = context =>
{
    string traceId = Activity.Current?.Id ?? context.HttpContext.TraceIdentifier;
    context.ProblemDetails.Extensions.TryAdd("traceId", traceId);
    context.HttpContext.Response.Headers["X-Correlation-ID"] = traceId;
});
builder.Services.Configure<ApiBehaviorOptions>(options => options.InvalidModelStateResponseFactory = context =>
{
    ValidationProblemDetails problem = new(context.ModelState)
    {
        Type = ApiProblemTypes.MalformedRequest,
        Title = "Malformed request",
        Status = StatusCodes.Status400BadRequest,
        Detail = "The request could not be parsed.",
    };
    problem.Extensions["code"] = ApiErrorCodes.MalformedRequest;

    return new BadRequestObjectResult(problem);
});

// SQLite files live under a configurable directory ("Database" section), outside wwwroot, so they are never
// addressable over HTTP. An explicit ConnectionStrings entry always wins over the configured default.
// See specs/adr/0013-sqlite-app-data-location.md.
DatabaseOptions database = builder.Configuration
    .GetSection(DatabaseOptions.SectionName)
    .Get<DatabaseOptions>() ?? new DatabaseOptions();
if (!string.IsNullOrWhiteSpace(database.DataDirectory) && database.Files.Count > 0)
{
    string dataDirectory = Path.IsPathRooted(database.DataDirectory)
        ? database.DataDirectory
        : Path.Combine(builder.Environment.ContentRootPath, database.DataDirectory);
    Dictionary<string, string?> defaultSqliteConnections = database.Files
        .Where(file => string.IsNullOrWhiteSpace(builder.Configuration.GetConnectionString(file.Key)))
        .ToDictionary(
            file => $"ConnectionStrings:{file.Key}",
            file => (string?)$"Data Source={Path.Combine(dataDirectory, file.Value)}");
    if (defaultSqliteConnections.Count > 0)
    {
        Directory.CreateDirectory(dataDirectory);
        builder.Configuration.AddInMemoryCollection(defaultSqliteConnections);
    }
}

// Create the directory for every resolved SQLite connection string (configured default or explicit override),
// so SQLite never fails with "unable to open database file" on a fresh or containerised deployment.
foreach (IConfigurationSection connection in builder.Configuration.GetSection("ConnectionStrings").GetChildren())
{
    SqliteConnectionDirectory.EnsureCreated(connection.Value);
}

// Modules.
builder.Services.AddInMemoryEventsBus();
builder.Services.AddWeatherModule(builder.Configuration);
builder.Services.AddFavoritesModule(builder.Configuration);
builder.Services.AddNotesModule(builder.Configuration);
builder.Services.AddGeographyModule(builder.Configuration);

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}
else
{
    // Blazor error page for UI requests; API requests are handled below.
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseHttpsRedirection();

// CSP Level 3 on every response; registered before static files/endpoints so all pages are covered.
app.UseCsp();

// API requests get the RFC 9457 Problem Details handler; UI requests fall through
// to the Blazor /Error handler above.
app.UseWhen(
    context => context.Request.Path.StartsWithSegments("/api"),
    api => api.UseExceptionHandler());

// Keep the Blazor not-found page off the API and CSP-report surfaces so their non-HTML responses stay intact.
app.UseWhen(
    context => !context.Request.Path.StartsWithSegments("/api")
               && !context.Request.Path.StartsWithSegments("/_csp"),
    ui => ui.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true));

app.UseAntiforgery();

app.MapStaticAssets();
app.MapControllers();

// CSP violation sink (unauthenticated). Report bodies are no-cors POSTs; no CORS policy is needed.
app.MapPost("/_csp/report", (HttpRequest request, ILoggerFactory loggerFactory) =>
{
    const long maxReportBytes = 16 * 1024;
    if (request.ContentLength is > maxReportBytes)
    {
        return Results.StatusCode(StatusCodes.Status413PayloadTooLarge);
    }

    loggerFactory.CreateLogger("CspReport").LogWarning(
        "CSP violation reported: contentType={ContentType} length={Length}",
        request.ContentType,
        request.ContentLength);

    return Results.NoContent();
}).DisableAntiforgery();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode(o => o.ContentSecurityFrameAncestorsPolicy = "'none'");

app.Run();

public partial class Program;
