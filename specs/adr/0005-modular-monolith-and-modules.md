# ADR-0005 — Modular monolith: bounded contexts, modules, and repo slicing

**Status:** Proposed (needs user confirmation)
**Amended by:** ADR-0012 (2025-10-15) — the separate `Weather.Api` host is merged into `Weather.WebUI`; the module rules below are unchanged.
**Amended by:** ADR-0016 (2025-10-15) — Favorites and Notes are merged into one `Journal` module; the bounded-context list and module table below are superseded, the module *rules* are unchanged.
**Supersedes:** ADR-0001 (Onion layout — the single set of layer projects is replaced by per-module layering)
**Date:** 2025-10-13 (revised)

## Context
The original split placed modules as *folders inside* four shared layer projects. That is not a modular
monolith: there is no assembly boundary, no per-module composition root, no data ownership by construction, and
only the compiler can't separate modules. The reference implementation
[kgrzybek/modular-monolith-with-ddd](https://github.com/kgrzybek/modular-monolith-with-ddd) shows the intended
shape (verified from the repo's live tree, 2025-10-13):

```
src/BuildingBlocks/{Domain,Application,Infrastructure}
src/Modules/<Module>/{Domain,Application,Infrastructure,IntegrationEvents}
src/Modules/<Module>/Tests/{ArchTests,UnitTests,IntegrationTests}
src/API/CompanyName.MyMeetings.API          # thin host
src/Tests/ArchTests                         # cross-module + API rules
```

MyMeetings business contexts: **UserAccess, Registrations, Meetings, Administration, Payments**.

Its stated rules (README §3.1): API contains no application logic; API talks to modules through a small
interface; **modules communicate only asynchronously via the Events Bus — direct calls are not allowed**; each
module owns its data in a separate schema; **a module may reference only another module's IntegrationEvents
assembly**; each module has its own composition root and is highly encapsulated.

Enforcement is executable (`src/Tests/ArchTests/Modules/ModuleTests.cs`): a module's Domain/Application/
Infrastructure assemblies must not depend on any other module's namespace — except `INotificationHandler<>` /
`*IntegrationEventHandler` types and `EventsBusStartup`.

## Step 1 — Identify the bounded contexts in *this* repo

Derived from the stated capabilities (not copied from MyMeetings):

| Requirement capability | Bounded context | Subdomain type | Evidence in requirements |
|------------------------|-----------------|----------------|--------------------------|
| Choose a country and city; browse the catalog | **Geography** | Supporting | `GET /api/countries`, `GET /api/countries/{code}/cities` |
| See current conditions for a city | **Weather** | **Core** | `GET /api/weather/{cityName}` + full metric payload |
| Save a city to favorites | **Favorites** | Supporting | `POST /api/cities/favorites` |
| Save a note about a city's weather | **Notes** | Supporting | `POST /api/weather/notes` |

**Explicitly rejected contexts** (requirements-driven, not arbitrary):
- **UserAccess / Identity** — MyMeetings has it because it authenticates users. This app has no login; adding it
  would create a module with no requirement. Rejected.
- **Alerts / Notifications** — no requirement.
- **Forecast / History** — current conditions only.
- A **"Common"/"Shared"** module — shared code is `BuildingBlocks`, a dependency, not a bounded context.

**Considered alternative:** merge Favorites + Notes into one `WeatherJournal` context. Rejected: different
aggregates (`FavoriteCity` vs `WeatherNote`), different invariants, different lifecycles — Grzybek keeps
`Registrations` and `UserAccess` separate on the same reasoning. Fallback documented.

## Step 2 — Module slicing decision

Adopt the reference structure. Each module is a **bounded context with its own assembly set**:
`Weather.Modules.<Module>.{Domain, Application, Infrastructure, IntegrationEvents}`.

```
src/
  BuildingBlocks/
    Weather.BuildingBlocks.Domain/          DDD primitives (Entity, AggregateRoot, ValueObject,
                                            TypedIdValueBase, DomainEventBase, IBusinessRule, ...)
    Weather.BuildingBlocks.Application/     ICommand/IQuery/CommandBase/QueryBase, validation,
                                            processing pipeline, EventsBus abstractions
    Weather.BuildingBlocks.Infrastructure/  EF base, Outbox, InMemoryEventsBus, UnitOfWork, Clock,
                                            domain-event dispatcher, module initializer helpers
  Modules/
    Weather.Modules.Geography/              {Domain, Application, Infrastructure, IntegrationEvents}
    Weather.Modules.Weather/                {Domain, Application, Infrastructure, IntegrationEvents}
    Weather.Modules.Favorites/              {Domain, Application, Infrastructure, IntegrationEvents}
    Weather.Modules.Notes/                  {Domain, Application, Infrastructure, IntegrationEvents}
  Weather.Api/                              thin host; one endpoint area per module
  Weather.WebUI/                            Blazor client (HTTP only)
```

### Module → assembly → data

| Module | Sliced assemblies | Own schema (SQLite file) | Context | Publishes | Consumes |
|--------|-------------------|--------------------------|---------|-----------|----------|
| Geography | 4 | `geography` | `GeographyContext` | `CityAddedIntegrationEvent` | — |
| Weather | 4 | (none) | — | — | — |
| Favorites | 4 | `favorites` | `FavoritesContext` | — | `CityAddedIntegrationEvent` |
| Notes | 4 | `notes` | `NotesContext` | — | `CityAddedIntegrationEvent` |

### Cross-module communication (async only)
```
Geography ──CityAddedIntegrationEvent──► EventsBus ──► Favorites  (updates its local City projection)
Geography ──CityAddedIntegrationEvent──► EventsBus ──► Notes      (updates its local City projection)
Weather   ── independent: OpenWeatherMap geocodes `q={city},{country}`; no module dependency
```
No module calls another module's Application/Domain directly. Only `*.IntegrationEvents` may be referenced.

## Step 3 — Preserving Onion inside the modular monolith
The original requirement's Onion layering is preserved **per module** (README §3.2: each module has Clean
Architecture). Within every module: `Domain ← Application ← Infrastructure`, with `IntegrationEvents` as the
published contract. The API host is the only presentation. This replaces the single set of layer projects.

## Deliberate deviations from the reference (documented)
1. **No per-module IoC container / Autofac.** The reference gives each module its own container; the
   `audit-modular-monolith` catalog marks that a near-singleton, contested tactic (REFERENCE.md §2 Tier 3). We
   use Microsoft DI with a per-module `Add<Module>Module()` + `Initialize()` extension. Boundaries are enforced
   by arch tests instead.
2. **In-process in-memory Events Bus** (no external broker), matching the reference's `InMemoryEventsBus`.
3. **SQLite** instead of SQL Server, so the "separate schema per module" is one file per module
   (ADR-0006), which is actually a stronger isolation than prefixing.

## Consequences
- Positive: module boundary == assembly boundary → the compiler enforces most of the boundary (rung 1).
- Positive: "only IntegrationEvents cross" is enforceable by `ModuleTests` exactly as in the reference.
- Positive: each module has its own schema/context → data ownership is structural.
- Negative: project count rises (~24 src projects). The reference's README explicitly allows merging
  Domain/Application/Infrastructure per module; that pragma is offered as a fallback in ADR-0005b note below.
- Negative: integration-event plumbing (outbox + bus + handlers) is real work; scoped in e01s02/e02s03/e04s02/e05s02.

## Fallback (if the user prefers fewer projects)
Merge each module's Domain/Application/Infrastructure into one `Weather.Modules.<Module>` assembly, keeping
`IntegrationEvents` separate. Boundaries then rely on arch tests rather than assembly references. The rest of
this plan is unchanged.
