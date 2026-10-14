# ADR-0009 — Controller-based APIs (ControllerBase) for the HTTP host

**Status:** Proposed (needs user confirmation)
**Date:** 2025-10-13

## Context
The host originally exposed endpoints as Minimal API lambdas (e.g. `Weather.Api/Modules/Weather/WeatherEndpoints.cs`),
which hand-rolls binding, routing, and result shaping. Microsoft's `fundamentals/apis.md` recommends Minimal APIs
for new projects but explicitly supports the controller-based approach, "preferred for large applications with
complex business logic, teams familiar with the MVC pattern, applications requiring specific MVC features"
(model-binding extensibility, advanced validation, application parts, OData).

`web-api/index.md` documents that API controllers derive from `ControllerBase` (not `Controller`, which adds
views) and that `[ApiController]` enables: **attribute routing is required**, **automatic HTTP 400** with
`ValidationProblemDetails`, and **binding-source inference** (`[FromRoute]`, `[FromQuery]`, `[FromBody]`).
`web-api/action-return-types.md` recommends **`ActionResult<T>`** when multiple result types are possible, with
`[ProducesResponseType]` for API docs.

## Decision
Use **controller-based APIs** for the host:

1. Controllers derive from a shared `ApiControllerBase : ControllerBase` annotated with `[ApiController]` and
   `[Produces("application/json")]`.
2. Attribute routing per module (`[Route("api/countries")]`, `[Route("api/weather")]`).
3. Return **`ActionResult<T>`** (`Ok(...)`, `NotFound(...)`, `Problem(...)`); annotate with
   `[ProducesResponseType]`.
4. Explicit binding sources (`[FromRoute]`, `[FromQuery]`); `CancellationToken` on every action.
5. Register with `builder.Services.AddControllers()` and map with `app.MapControllers()`.
6. Minimal API endpoint classes are removed; the API host stays thin — an action binds, calls the module's
   `I<Module>Service`, and maps the result. No application logic in controllers.

Controllers are grouped by module under `Weather.Api/Modules/<Module>/`, preserving the modular-monolith layout.

## Rationale
- Consistency and discoverability: one API style across the host.
- Built-in MVC behaviours (routing, binding, `ActionResult`, problem details) instead of hand-rolled lambdas.
- Better fit for the project's growing module surface and for MVC features (filters, application model).

## Consequences
- Positive: uniform, idiomatic API surface; `[ApiController]` behaviours (400 `ValidationProblemDetails`,
  binding source inference) come for free.
- Positive: `ActionResult<T>` + `[ProducesResponseType]` give well-typed, documented responses.
- Negative: slightly more ceremony than Minimal APIs; marginally more overhead (acceptable here).
- **Migration:** the committed `WeatherEndpoints` (Minimal API) is migrated to a `WeatherController` **as part of
e02** (same routes), so the host is uniformly controller-based once this slice lands.
- **OpenAPI:** enabled now — `builder.Services.AddOpenApi()` + `app.MapOpenApi()` (Swagger UI in Development),
  with `[ProducesResponseType]` on actions; builds on the `ActionResult<T>` typing.
- **Controller unit tests:** a new `Weather.Api.UnitTests` project instantiates each controller with a stubbed
  `I<Module>Service` to assert `Ok`/`NotFound` result types, in addition to the host integration tests.
- **Consistent module API naming:** module APIs use `I<Module>Service`. As part of e02, the Weather module is
  aligned: its provider port is renamed `IWeatherService` → **`IWeatherProvider`**, and its module API
  `IWeatherModule` → **`IWeatherService`** (impl `WeatherModule` → `WeatherService`; `AddWeatherModule` unchanged).
- FluentValidation still runs in the MediatR pipeline and surfaces as `ValidationException` → 400 via
  `GlobalExceptionHandler`; `[ApiController]`'s automatic 400 covers ModelState/DataAnnotations binding errors.
  The two are complementary and produce the same `ProblemDetails` shape.

## Alternatives rejected
- **Minimal APIs (current):** rejected by the user; hand-rolled endpoint classes duplicate framework behaviour.
- **Mixed styles:** inconsistent; hard to review; rejected.
