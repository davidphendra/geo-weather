# Weather App — Implementation Plan (Modular Monolith · CQRS · Onion-per-module)

Status: **PLANNING COMPLETE — immediate work is the e02 Geography module read side (self-contained).**
Model: `kgrzybek/modular-monolith-with-ddd` — a module = a bounded context = its own assembly set + own
composition root + own schema; modules integrate **only** asynchronously via integration events.

## How the reference divides modules (the method)
MyMeetings modules are bounded contexts: **UserAccess, Registrations, Meetings, Administration, Payments**. Each
has `Domain / Application / Infrastructure / IntegrationEvents` assemblies, its own DbContext/schema, its own
composition root, and one `I<Module>Service` interface used by the API host. Cross-module rules (verbatim from
its README and `ModuleTests.cs`):
- API has no application logic; it sends commands/queries to exactly one module.
- **Modules communicate only asynchronously via the Events Bus — direct calls are not allowed.**
- A module may reference another module **only** through its `IntegrationEvents` assembly.
- One schema per module; shared data is not allowed.

## Bounded contexts in *this* repo (derived from the requirements)

| Module | Kind | Own data | Publishes | Consumes | Requirement it serves |
|--------|------|----------|-----------|----------|-----------------------|
| **Geography** | Supporting | `geography.db` | `CityAddedIntegrationEvent` | — | country/city dropdowns |
| **Weather** | **Core** | none (external) | — | — | view current weather |
| **Favorites** | Supporting | `favorites.db` | — | `CityAddedIntegrationEvent` | save favorite cities |
| **Notes** | Supporting | `notes.db` | — | `CityAddedIntegrationEvent` | save weather notes |

**Rejected contexts:** UserAccess/Identity (the reference has it because it authenticates — this app has no
login); Alerts/Notifications; Forecast/History. A "Common" module is not a context — shared code is
`BuildingBlocks`. Alternative considered: merge Favorites+Notes into one `WeatherJournal` context (rejected:
different aggregates/invariants; fallback documented).

## Repo slicing

```
src/
  BuildingBlocks/
    Weather.BuildingBlocks.Domain/          Entity, AggregateRoot, ValueObject, TypedIdValueBase,
                                            DomainEventBase, IDomainEvent, IBusinessRule, IAggregateRoot
    Weather.BuildingBlocks.Application/     ICommand, IQuery, CommandBase, QueryBase, validation, pipeline
    Weather.BuildingBlocks.Infrastructure/  EF base, Outbox, InMemoryEventsBus, UnitOfWork, Clock
  Modules/
    Weather.Modules.Geography/{Domain,Application,Infrastructure,IntegrationEvents}
    Weather.Modules.Weather/{Domain,Application,Infrastructure,IntegrationEvents}
    Weather.Modules.Favorites/{Domain,Application,Infrastructure,IntegrationEvents}
    Weather.Modules.Notes/{Domain,Application,Infrastructure,IntegrationEvents}
  Weather.Api/            thin host; one endpoint area per module
  Weather.WebUI/          Blazor client (HTTP only)
src/Modules/<M>/Tests/{UnitTests,IntegrationTests,ArchTests}
src/Tests/ArchTests/      ModuleTests (cross-module) + ApiTests
src/Tests/IntegrationTests/  end-to-end host tests
```

## Module context map

```
Geography ──CityAddedIntegrationEvent──► EventsBus ──► Favorites (local City projection)
        └──CityAddedIntegrationEvent──► EventsBus ──► Notes     (local City projection)
Weather   ── independent: provider geocodes q={city},{country}
```

## Data isolation
One SQLite file per persisting module (`geography.db`, `favorites.db`, `notes.db`), one `DbContext` each, no
cross-module FK/join. This is effectively data-isolation **level 3** on SQLite — stronger than the reference's
one-database/separate-schemas (ADR-0006). Favorites/Notes keep a small local `City` projection.

## Decisions to confirm

| # | Decision | Recommendation | Rejected alternative |
|---|----------|----------------|----------------------|
| D1 | Module slicing | Reference module-per-assembly (Domain/Application/Infrastructure/IntegrationEvents each) | Modules as folders inside shared layer projects |
| D2 | Module set | 4 contexts: Geography, Weather, Favorites, Notes | Merge Favorites+Notes → WeatherJournal |
| D3 | Granularity fallback | Full 4 assemblies per module | 1 module assembly + separate IntegrationEvents |
| D4 | Front-end | Keep/reshape Blazor `Weather.WebUI` | Replace with plain JS/React |
| D5 | Persistence | One SQLite file per module | Single shared SQLite file |
| D6 | Dew point | Magnus formula in the Weather domain | OpenWeatherMap One Call 3.0 (paid) |
| D7 | MediatR | Pin 12.x (Apache-2.0) | v13 (commercial license change) |

Full rationale: `specs/adr/0005` and `0006`; D4–D7 in ADRs 0002–0004.

## Epics (WSJF order) — 9 epics / 18 stories / 79 BCPs

| Epic | Title | Module(s) | Stories | BCPs | WSJF |
|------|-------|-----------|---------|------|------|
| e00 | Repository Layout Scaffolding | — (layout) | e00s01 | 8 | 10.0 |
| e01 | BuildingBlocks & Modular Host Skeleton | BB, host | e01s01, e01s02 | 10 | 9.5 |
| e03 | Weather Module (core) | M2 | e03s01–03 | 13 | 8.5 |
| e02 | Geography Module | M1 | e02s01–03 | 11 | 7.5 |
| e06 | WebUI Front-End | — | e06s01, e06s02 | 10 | 6.5 |
| e04 | Favorites Module | M3 | e04s01, e04s02 | 8 | 6.0 |
| e05 | Notes Module | M4 | e05s01, e05s02 | 8 | 5.5 |
| e07 | Architecture & Offline Test Gates | all | e07s01, e07s02 | 8 | 5.0 |
| e08 | Documentation & Repository Tooling | — | e08s01 | 3 | 3.0 |

Index: `specs/release-plan.yaml`. Capsules: `specs/epics/eNN-*/`.

## Endpoints

| Method | Route | Module | Notes |
|--------|-------|--------|-------|
| GET | `/api/countries` | Geography | AsNoTracking |
| GET | `/api/countries/{countryCode}/cities` | Geography | AsNoTracking; 404 unknown |
| GET | `/api/weather/{cityName}` | Weather | full payload; 404/502 |
| POST | `/api/weather/notes` | Notes | own DB; 201 + id; 400 |
| POST | `/api/cities/favorites` | Favorites | own DB; 201 + id; 409 |

## Test strategy (offline)
Per-module unit + integration tests; per-module `LayersTests` + `DomainTests`; cross-module `ModuleTests`;
host `ApiTests`; end-to-end `src/Tests/IntegrationTests` with stub provider and per-module `:memory:` contexts.
See `specs/tech-architecture/TEST_PLAN_LATEST.md`.

## Execution order
e00s01 (layout) → e01s01 → e01s02 → e02s01 → e02s02 → e02s03 → e03s01 → e03s02 → e03s03 → e04s01 → e04s02 →
e05s01 → e05s02 → e06s01 → e06s02 → e07s01 → e07s02 → e08s01.
Per story: `develop-tdd` → `verify-work` → `audit-code` → `commit-message` → `release-branch`.

## Immediate scope — e02 Geography module (read side)
Build the Geography module (M1) read side and nothing else. It is self-contained per ADR-0008 — no
BuildingBlocks; plain sealed domain types; MediatR `IRequest<T>`; a module-local `ValidationBehavior`; EF Core
in the module's own Infrastructure with `geography.db`. Scope: `e02s01` (Country + `GET /api/countries`) and
`e02s02` (City + `GET /api/countries/{countryCode}/cities`), both `AsNoTracking()`.

The **`CityAddedIntegrationEvent` (e02s03) is deferred** — it needs the shared Outbox + In-memory Events Bus,
which belongs with the write-side modules (Favorites/Notes). No cross-module reference is introduced.
BuildingBlocks (e01) and every other module remain out of scope.

## Requirement coverage map

| Requirement | Where |
|-------------|-------|
| Modular monolith / bounded contexts | e01s02 (skeleton), e02/e03/e04/e05 (modules), e07s01 (arch gates) |
| Onion architecture | inside every module (`LayersTests`) |
| CQRS via MediatR | each module's `Application` |
| Read endpoints + AsNoTracking | e02, e03 |
| POST + ET persistence + id return | e04, e05 (own schemas) |
| OpenWeatherMap via IWeatherProvider | e03s02 |
| F→C conversion | e03s01 |
| Full payload (wind/visibility/pressure/sky/dew point/humidity) | e03s03 |
| Front-end workflow | e06 |
| Unit / handler / integration / offline tests | e02–e07 |
| AI instructions file | e01s01 (`AGENTS.md`/`CLAUDE.md` + `CONVENTIONS.md`), e08s01 |
| README + GitHub repo | e08s01 |

## Open items
- 7 decisions (D1–D7) must be confirmed.
- OWM API key needed only for live runs (user-secrets); tests never need it.
- `git init` / remote / first commit deferred (user asked not to commit).
- Two documented deviations from the reference: Microsoft DI instead of per-module Autofac (ADR-0005); SQLite
  files instead of SQL Server schemas (ADR-0006).
