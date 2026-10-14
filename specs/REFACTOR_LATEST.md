# Refactor Plan — Weather.WebUI host composition (`Program.cs`)

> Status: approved (interview Q1–Q5). One commit (Q4). Strictly behavior-neutral (Q3).

## Problem Statement

`src/Weather.WebUI/Program.cs` had grown into a ~158-line block of top-level statements that mixed five
unrelated concerns in an order-sensitive sequence:

1. service registration (Blazor, REST, CSP, `HttpClient`, page models);
2. error-handling wiring (a `ProblemDetails` customization and the `ApiBehaviorOptions` factory, both
   large inline lambdas);
3. SQLite connection-string composition and directory creation;
4. module registration;
5. the middleware pipeline;
6. endpoint mapping (static assets, controllers, an `/api` fallback, the CSP sink, Blazor), with more
   inline lambdas, and magic strings throughout.

A reader had to hold all six concerns in their head at once, and the correctness of the app depended on
the *order* of statements scattered among unrelated ones. It was hard to read, hard to review, and easy
to break (BUG-001 was caused by an ordering subtlety in this file).

## Solution

Reduce `Program.cs` to a short orchestration that reads top-to-bottom, and move each concern into a
named extension method in a new `Weather.WebUI.Hosting` area. Extract the four inline lambdas into
named handler classes so nothing is anonymous.

Target `Program.cs` (~16 lines):

```
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddWeatherUi(builder.Configuration);   // Blazor + REST + CSP + client + page models
builder.Services.AddApiErrorHandling();                  // exception handler + ProblemDetails + ApiBehavior
builder.AddModuleDatabases();                            // SQLite connection strings + directories
builder.Services.AddWeatherModules(builder.Configuration);

var app = builder.Build();

app.UseWeatherUiPipeline();     // dev/prod handling, HTTPS, CSP, /api handler, status pages, antiforgery
app.MapWeatherUiEndpoints();    // static assets, controllers, /api fallback, CSP sink, Blazor

app.Run();

public partial class Program;   // required by WebApplicationFactory
```

### Invariant that MUST be preserved

The **middleware order** and the **endpoint mapping** are behavior-sensitive (BUG-001 depends on the
not-found re-execute running in the main pipeline, and on the `/api` fallback carrying a body). The
extraction is mechanical: identical logic, identical order, identical strings. `public partial class
Program;` stays so `WeatherHostFactory` keeps working.

## Commits

One commit (Q4 = C):

1. Extract the six concerns into `Weather.WebUI.Hosting` extension methods and named handler classes,
   leaving `Program.cs` as a short orchestration → verify: `dotnet build Weather.sln --warnaserror && dotnet format Weather.sln --verify-no-changes && dotnet test Weather.sln`

## Decision Document

- **New namespace/folder:** `Weather.WebUI.Hosting` (`src/Weather.WebUI/Hosting/`).
- **Units to create:**
  - `WebUiServiceCollectionExtensions.AddWeatherUi(IServiceCollection, IConfiguration)` — Blazor
    (`AddRazorComponents().AddInteractiveServerComponents()`), REST (`AddControllers`, `AddOpenApi`),
    `AddCsp`, `AddHttpClient<WeatherApiClient>`, and the scoped page models.
  - `ApiErrorHandlingExtensions.AddApiErrorHandling(IServiceCollection)` — `AddExceptionHandler<…>`,
    `AddProblemDetails`, `Configure<ApiBehaviorOptions>`.
  - `ModuleDatabaseExtensions.AddModuleDatabases(WebApplicationBuilder)` — the `Database` section
    resolution, connection-string defaults, and `SqliteConnectionDirectory.EnsureCreated`.
  - `ModuleRegistrationExtensions.AddWeatherModules(IServiceCollection, IConfiguration)` — events bus +
    the four modules.
  - `WebUiPipelineExtensions.UseWeatherUiPipeline(WebApplication)` — dev/prod exception handler + HSTS,
    HTTPS redirection, CSP, the `/api` exception-handler branch, the not-found re-execute, antiforgery.
  - `WebUiEndpointExtensions.MapWeatherUiEndpoints(WebApplication)` — dev OpenAPI, static assets,
    controllers, `/api` fallback, `/_csp/report`, Razor components (with the frame-ancestors option).
- **Named handler classes (Q1 = B):** `ProblemDetailsCustomization`, `ApiBehaviorCustomization`,
  `CspReportEndpoint`, `ApiNotFoundEndpoint`.
- **Placement of `MapOpenApi`:** moves from the dev/prod `if` in the pipeline into the endpoint mapping
  (it is an endpoint, not middleware). Endpoint registration still happens before `Run`, so behavior is
  unchanged.
- **Visibility:** the new host-only types are `internal` (consistent with `GlobalExceptionHandler`,
  `SqliteConnectionDirectory`).
- **No interfaces/abstractions** are introduced — extension methods and static handlers only.
- **No public contract changes:** the HTTP surface, DI lifetimes, module registration, and error handling
  are identical.
- **No behavior changes and no string cleanup** (Q3 = A). Magic strings stay as-is; a later change may
  introduce constants.

## Testing Decisions

- No new tests (Q5 = A). The refactor is behavioral-neutral and the existing suite already covers every
  moved seam: `/`, `/favorites`, `/not-found` (BUG-001), unmatched `/api` ProblemDetails, CSP
  headers/nonce/report, the `Database` connection-string composition, and the architecture rules.
- Evidence required: `dotnet build --warnaserror`, `dotnet format --verify-no-changes`, `dotnet test
  Weather.sln` (248 tests), and `npm run parity:styles` (7 tests) all green after the change.

## Out of Scope

- Any behavioral change (e.g. tightening the `/api` fallback, reordering middleware).
- Introducing configuration/route constants to replace magic strings.
- Extracting a `Startup` class, `IHostModule` abstraction, or per-feature registration modules.
- Touching module registration internals, `CspPolicy`, or the page components.
- Adding new tests.

## Further Notes

- Prior art: the modules already follow this shape — each exposes `Add<Module>Module()` composition
  extensions; the host now does the same for its own concerns.
- This is a "Tidy First" structural change only; if a defect is discovered while extracting, it is logged
  (fix-or-log) rather than fixed inside this refactor.
