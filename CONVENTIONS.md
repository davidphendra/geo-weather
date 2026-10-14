# Conventions

Shared rules for all AI agents working in this repository. Read before any git or GitHub operation.

## Always Green / Shift Left

A defect costs 1 unit to fix at author time, 10 units at review, 100 units in production. Keep Preflight green.

- **Preflight:** `dotnet build Weather.sln --warnaserror && dotnet format Weather.sln --verify-no-changes && dotnet test Weather.sln`
- **CI:** `gh pr checks` when a PR is open.
- Preflight and CI MUST be green before forward work.

## Discovered Defects

Fix-or-log, in order:

1. If the fix is small and safe, fix it now in a **separate commit**.
2. Otherwise open a bug record in `specs/bugs/registry.yaml` (`quick-fix` → `fix-bug`) and continue.

Never ignore a red gate. Never leave a discovered defect unlogged.

### Banned dismissive phrases

| Phrase | Why it is banned |
|--------|------------------|
| "pre-existing" | A red gate is red regardless of who broke it. |
| "unrelated to my changes" | The build is the team's, not the commit's. |
| "not introduced by my changes" | Ownership follows the gate, not the diff. |
| "out of scope" | Log it or fix it; do not wave it away. |

## C# Standards

All C# code MUST follow `docs/engineering/csharp-standards.md` (mandatory) in addition to SOLID, DRY, and KISS.
Machine-enforced by `Directory.Build.props` (`Nullable`, `TreatWarningsAsErrors`, `AnalysisLevel=latest`,
`EnforceCodeStyleInBuild`) and `.editorconfig`.

Highlights you MUST apply:

- `net10.0` with the framework's default language version — NEVER `<LangVersion>latest</LangVersion>`.
- Nullable enabled; model optionals explicitly; NEVER use the null-forgiving operator to hide uncertainty.
- File-scoped namespaces; one type per file; `sealed` classes not meant for inheritance.
- Records for value data/DTOs; classes for identity/lifecycle; interfaces only at real boundaries.
- Guard clauses at boundaries (`ArgumentNullException.ThrowIfNull`, `ArgumentException.ThrowIfNullOrWhiteSpace`,
  `ArgumentOutOfRangeException.ThrowIfNegative`); enforce invariants in the domain; do not duplicate validation.
- `ILogger<T>` structured logging; NEVER log secrets, tokens, or API keys.
- Async for I/O with `CancellationToken`; no `.Result`/`.Wait()`; no `async void`; no blind `ConfigureAwait(false)`.
- External I/O: timeouts, cancellation, bounded transient retries with jitter (`Microsoft.Extensions.Http.Resilience`).
- `decimal` for exact arithmetic; UTC/`DateTimeOffset` for instants.

## Modular Monolith Conventions

One deployable unit. A **module = bounded context = its own assembly set** + its own composition root + its own
data. Reference model: `kgrzybek/modular-monolith-with-ddd`.

| Module | Assemblies | Own schema | Context | Publishes | Consumes |
|--------|-----------|-----------|---------|-----------|----------|
| M1 Geography | `Weather.Modules.Geography.*` | `geography.db` | `GeographyContext` | `CityAddedIntegrationEvent` | — |
| M2 Weather | `Weather.Modules.Weather.*` | (none) | — | — | — |
| M3 Favorites | `Weather.Modules.Favorites.*` | `favorites.db` | `FavoritesContext` | — | `CityAddedIntegrationEvent` |
| M4 Notes | `Weather.Modules.Notes.*` | `notes.db` | `NotesContext` | — | `CityAddedIntegrationEvent` |

Each module has `Domain`, `Application`, `Infrastructure`, `IntegrationEvents` assemblies. `BuildingBlocks`
(`Weather.BuildingBlocks.{Domain,Application,Infrastructure}`) hold shared primitives.

Boundary rules — a violation is a build-breaking defect:

- A module MUST reference another module ONLY through that module's `IntegrationEvents` assembly.
- Modules MUST communicate only asynchronously via the Events Bus. Direct cross-module calls are forbidden.
- Permitted cross-module flows: Geography → Favorites and Geography → Notes (`CityAddedIntegrationEvent`). Weather is independent.
- Each module owns its data: own `DbContext`, own SQLite file, no cross-module joins or foreign keys.
- Cross-module reference is forbidden in the API host too: each API area talks to one module only.
- Modules are initialized by the host via `Add<Module>Module()` + `Initialize(...)`.
- Enforced by `ModuleTests` (cross-module), `LayersTests` (per module), and `ApiTests` (host) — NetArchTest.

## Onion Layering (inside every module)

| Layer | May reference | MUST NOT reference |
|-------|---------------|--------------------|
| `<Module>.Domain` | BCL, `BuildingBlocks.Domain` | Application, Infrastructure, EF Core, ASP.NET |
| `<Module>.Application` | own Domain, `BuildingBlocks.Application`, MediatR, FluentValidation | own Infrastructure, EF Core |
| `<Module>.Infrastructure` | own Domain/Application, `BuildingBlocks.Infrastructure`, EF Core, `HttpClient` | other modules |
| `<Module>.IntegrationEvents` | BCL | anything module-internal |
| `Weather.Api` | each module's `Application.Contracts` | another module's namespace from an API area |
| `Weather.WebUI` | HTTP contract of Api only | any server project |

- Dependencies point inward within a module. Domain has zero external framework dependencies.
- The API host is thin: bind → `I<Module>Module` → map result. No application logic in endpoints.

## CQRS Patterns

- One request type per file. Naming: `GetXxxQuery`, `AddXxxCommand`, `UpdateXxxCommand`.
- Handlers: `XxxQueryHandler`, `XxxCommandHandler`, implementing `IRequestHandler<TRequest, TResponse>`.
- Queries return DTOs (`record`) — never domain entities.
- Commands return the created resource identifier (`Guid`).
- Cross-cutting concerns live in MediatR `IPipelineBehavior` (validation, logging), not in handlers.
- Validation lives in FluentValidation `AbstractValidator<TRequest>` next to the request.
- Each module exposes exactly one `I<Module>Module` interface with `ExecuteCommandAsync` / `ExecuteQueryAsync`.

## Naming Conventions

- Projects: `Weather.BuildingBlocks.<Layer>`, `Weather.Modules.<Module>.<Layer>`, `Weather.Api`, `Weather.WebUI`.
- Modules: `Geography`, `Weather`, `Favorites`, `Notes` (bounded-context names, never technical names).
- Folders mirror layers/features; files are one-type-per-file.
- Interfaces: `IWeatherService`, `ICityRepository`, `IDateTimeProvider`.
- Async methods end in `Async`; take a `CancellationToken`.
- Private fields `_camelCase`; constants `PascalCase`.

## Data Access Rules

- Every read query uses `.AsNoTracking()`.
- EF Core entity configuration lives in `IEntityTypeConfiguration<T>` classes, one per entity.
- Domain entities have no EF attributes. Infrastructure maps them fluently.
- Persistence is SQLite, one file per persisting module (`geography.db`, `favorites.db`, `notes.db`).
- Tests use the EF Core InMemory provider or SQLite `:memory:` per module.
- Each persisting module owns a `DbContext`; migrations are per-context.
- A module may store only a **local projection** of another module's reference data; it must not query another module's tables.

## Testing Rules (F.I.R.S.T)

- **F**ast, **I**solated, **R**epeatable, **S**elf-validating, **T**imely.
- Unit tests mock dependencies (`NSubstitute`); no I/O, no network.
- Command handler tests assert on EF Core InMemory state changes.
- Integration tests use `WebApplicationFactory` with a stub `IWeatherService`; zero live network calls.
- Architecture tests verify both layer direction and module boundaries (acyclicity + contract-only access).
- At least one happy path and one edge case per story.

## Defensive Code Categories

- **Retry:** transient HTTP failures to OpenWeatherMap (bounded, e.g. 3 attempts).
- **Timeout:** outbound weather calls bounded (e.g. 10s).
- **Graceful degradation:** weather endpoint returns `502` ProblemDetails when upstream is unavailable.
- Not used in this project: rate limit, circuit breaker (single external provider, in-process cache is out of scope).

## Specs Output Convention

All planning, scope, decisions, and verification artifacts go under `specs/`. No code without a story in `specs/epics/`.
