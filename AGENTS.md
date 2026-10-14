<!-- BEGIN bigpowers:project -->
# Weather — AI Agents

Read `CONVENTIONS.md` before any git or GitHub operation.

## Project
Weather app: ASP.NET Core Web API (Onion + CQRS/MediatR) plus a Blazor WebUI client. A user picks a country and city, views current weather, and saves weather notes / favorite cities.
Stack: C# / .NET 10 / ASP.NET Core / Blazor / EF Core (SQLite) / MediatR.

## Commands
| Action | Command |
|--------|---------|
| Restore | `dotnet restore Weather.sln` |
| Build | `dotnet build Weather.sln --warnaserror` |
| Test | `dotnet test Weather.sln` |
| Lint | `dotnet format Weather.sln --verify-no-changes` |
| Preflight | `dotnet build Weather.sln --warnaserror && dotnet format Weather.sln --verify-no-changes && dotnet test Weather.sln` |
| Run API | `dotnet run --project src/Weather.Api` |
| Run WebUI | `dotnet run --project src/Weather.WebUI` |

## Architecture
Modular monolith sliced by bounded context (reference: kgrzybek/modular-monolith-with-ddd). BuildingBlocks (`Weather.BuildingBlocks.{Domain,Application,Infrastructure}`) hold shared DDD/infra primitives. Each module is a full assembly set: `Weather.Modules.<Module>.{Domain, Application, Infrastructure, IntegrationEvents}` with its own composition root and data. Modules: **Geography**, **Weather** (core), **Favorites**, **Notes**. Each module is internally Onion-layered and uses CQRS (MediatR). A thin `Weather.Api` host exposes one endpoint area per module and calls that module through its `I<Module>Service` interface. `Weather.WebUI` is the Blazor client and uses only the API's HTTP contract. See `specs/adr/0005` and `0006`.

## Conventions
- Each module is internally layered: Domain ← Application ← Infrastructure. Domain has no framework dependency.
- A module MUST reference another module ONLY through its `IntegrationEvents` assembly.
- Modules MUST communicate only asynchronously via the Events Bus. Direct cross-module calls are forbidden.
- Each module owns its data: its own `DbContext` and its own SQLite file. No shared tables or cross-module joins.
- The API host is thin: bind → `I<Module>Service` → map result. No application logic in endpoints.
- Every EF Core read query MUST use `.AsNoTracking()`.
- Temperature is stored in the Weather domain in Celsius; Fahrenheit arrives only at the infrastructure boundary.
- Update `specs/` before writing code.

## Never
- Never dismiss a reproducible gate failure as pre-existing or out of scope.
- Never proceed on a red Preflight — invoke `quick-fix` or `fix-bug` first.
- Never make a live network call in tests; never commit an API key.
- Never reference a module's Domain/Application/Infrastructure from another module; only its `IntegrationEvents`.
- Never make a direct cross-module method call; use the Events Bus.
- Never let one module read another module's tables or `DbContext`.
- Never put application logic in `Weather.Api`; delegate to the owning module.

## Agent Rules
- **Workflow Mandate:** Use the bigpowers skills (`plan-work`, `develop-tdd`, `verify-work`) for work. Do not write feature code directly from a prompt.
- Read `specs/` before writing code.
- Write the minimum code that solves the stated problem. Nothing extra.
- Run tests after every change and show evidence before declaring done.
- One clarifying question beats a wrong assumption baked into 200 lines.
<!-- END bigpowers:project -->

<!-- BEGIN bigpowers:csharp-standards -->
## C# Engineering Standards (mandatory)
Full reference: `docs/engineering/csharp-standards.md`. Enforced by `Directory.Build.props` (`Nullable`, `TreatWarningsAsErrors`, `AnalysisLevel=latest`, `EnforceCodeStyleInBuild`) and `.editorconfig`.

- Target `net10.0` with the framework's default language version. NEVER set `<LangVersion>latest</LangVersion>`.
- Enable nullable reference types. Model optional data explicitly. NEVER use the null-forgiving operator to hide uncertainty.
- Use `sealed` on classes not designed for inheritance. One type per file. File-scoped namespaces.
- Use records for value-oriented data/DTOs; classes for identity or mutable lifecycle; interfaces only at real boundaries.
- Naming: PascalCase types/members, camelCase locals/params, `_camelCase` private fields, `I` interfaces, `Async` suffix.
- Validate untrusted input at boundaries with guard clauses (`ArgumentNullException.ThrowIfNull`, `ArgumentException.ThrowIfNullOrWhiteSpace`, `ArgumentOutOfRangeException.ThrowIfNegative`). Enforce invariants in the domain. Do not duplicate validation across layers.
- Exceptions: specific types, never swallowed, never for expected control flow; preserve inner exceptions; `ProblemDetails` at the API; no stack traces or secrets to clients.
- Async: `async`/`await` for all I/O; propagate `CancellationToken`; no `.Result`/`.Wait()`; no `Task.Run` over I/O; no `async void`; no unbounded fire-and-forget; no blind `ConfigureAwait(false)` in ASP.NET Core.
- DI: correct lifetimes; never capture scoped in singleton; `IHttpClientFactory` for HTTP; dispose what you own.
- Logging: `ILogger<T>` with structured properties; NEVER log secrets, tokens, or API keys; use appropriate levels.
- EF Core: async + cancellation; parameterized only; project only needed columns; `AsNoTracking()` for reads; no N+1.
- External I/O: timeouts, cancellation, retry only transient failures with bounded backoff + jitter; prefer `Microsoft.Extensions.Http.Resilience`.
- Tests: behaviour-focused, deterministic, isolated; cover boundaries and failure paths; never weaken a test to get green.
- Do not refactor unrelated code. Report commands run and results honestly; never claim a build/test passed unless it was actually run.
<!-- END bigpowers:csharp-standards -->

<!-- BEGIN bigpowers:rest-api-standards -->
## REST API Response Standards (mandatory)
Full reference: `docs/engineering/rest-api-standards.md` (ADR-0011).
- HTTP status code is the primary outcome signal. NEVER return 200 for a failed operation.
- Success returns the resource directly — no `success: true` envelope. Create → `201` + `Location` + the created resource; delete → `204`.
- Errors use RFC 9457 Problem Details (`application/problem+json`) with `type`, `title`, `status`, `detail`, `instance` plus `code` and `traceId`.
- Validation: FluentValidation → `422` `VALIDATION_FAILED` with field-level `errors[]`; model binding/ModelState → `400` `MALFORMED_REQUEST`.
- Stable error codes: `MALFORMED_REQUEST`, `VALIDATION_FAILED`, `RESOURCE_NOT_FOUND`, `DUPLICATE_RESOURCE`, `DEPENDENCY_UNAVAILABLE`, `INTERNAL_ERROR`.
- Entity mapping: not found → `404 RESOURCE_NOT_FOUND`; duplicate → `409 DUPLICATE_RESOURCE`; upstream failure → `502 DEPENDENCY_UNAVAILABLE`; unhandled → `500 INTERNAL_ERROR`.
- `traceId` from `Activity.Current?.Id` (fallback `HttpContext.TraceIdentifier`), added centrally; never return stack traces, SQL, or secrets.
- `[ProducesResponseType]` on every action (success + errors); document the contract with OpenAPI.
<!-- END bigpowers:rest-api-standards -->

<!-- BEGIN bigpowers:context-routing -->
## Context routing
| Path | Read first |
|------|-----------|
| `src/Weather.Domain/**` | `specs/product/GLOSSARY_LATEST.yaml` |
| `src/Weather.Application/**` | `specs/epics/e03-weather-read/`, `specs/epics/e04-write-side/` |
| `src/Weather.Infrastructure/**` | `specs/adr/0003-weather-provider-and-dew-point.md` |
| `tests/**` | `specs/tech-architecture/TEST_PLAN_LATEST.md` |
<!-- END bigpowers:context-routing -->

<!-- BEGIN bigpowers:learned-preferences -->
## Learned User Preferences

## Workspace Facts
<!-- END bigpowers:learned-preferences -->
