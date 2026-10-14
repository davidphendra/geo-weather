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
Modular monolith sliced by bounded context (reference: kgrzybek/modular-monolith-with-ddd). BuildingBlocks (`Weather.BuildingBlocks.{Domain,Application,Infrastructure}`) hold shared DDD/infra primitives. Each module is a full assembly set: `Weather.Modules.<Module>.{Domain, Application, Infrastructure, IntegrationEvents}` with its own composition root and data. Modules: **Geography**, **Weather** (core), **Favorites**, **Notes**. Each module is internally Onion-layered and uses CQRS (MediatR). A thin `Weather.Api` host exposes one endpoint area per module and calls that module through its `I<Module>Module` interface. `Weather.WebUI` is the Blazor client and uses only the API's HTTP contract. See `specs/adr/0005` and `0006`.

## Conventions
- Each module is internally layered: Domain ← Application ← Infrastructure. Domain has no framework dependency.
- A module MUST reference another module ONLY through its `IntegrationEvents` assembly.
- Modules MUST communicate only asynchronously via the Events Bus. Direct cross-module calls are forbidden.
- Each module owns its data: its own `DbContext` and its own SQLite file. No shared tables or cross-module joins.
- The API host is thin: bind → `I<Module>Module` → map result. No application logic in endpoints.
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
