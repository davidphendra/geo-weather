# Test Plan

> Hard constraint: **all automated tests run offline with zero live network calls.**

## Layout (mirrors the reference repo)
Tests live per module (`src/Modules/<Module>/Tests/`), plus global cross-module/API arch tests (`src/Tests/`).

```
src/BuildingBlocks/Tests/                    BuildingBlocks unit + integration tests
src/Modules/Geography/Tests/                 UnitTests/  IntegrationTests/  ArchTests/
src/Modules/Weather/Tests/                   UnitTests/  IntegrationTests/  ArchTests/
src/Modules/Favorites/Tests/                 UnitTests/  IntegrationTests/  ArchTests/
src/Modules/Notes/Tests/                     UnitTests/  IntegrationTests/  ArchTests/
src/Tests/ArchTests/                         ModuleTests (cross-module) + ApiTests
src/Tests/IntegrationTests/                  end-to-end host tests (offline)
```

## Test pyramid
| Level | Where | Scope |
|-------|-------|-------|
| Domain unit | each module `Tests/Domain.UnitTests` | conversion math, dew point, invariants, aggregate rules |
| Application unit | each module `Tests/UnitTests` | query handlers (mocked ports), command handlers (EF InMemory) |
| Module integration | each module `Tests/IntegrationTests` | module context + handler pipeline |
| Architecture | each module `Tests/ArchTests` + `src/Tests/ArchTests` | per-module layers, cross-module isolation, API isolation |
| End-to-end | `src/Tests/IntegrationTests` | host through `WebApplicationFactory`, stub provider, SQLite `:memory:` |
| Observability (`Category=Observability`) | module tests + `Weather.WebUI.UnitTests` + `Weather.IntegrationTests` | metric/trace assertions via BCL `MeterListener`/`ActivityListener`; exporters disabled offline (ADR-0014) |

## Architecture rules (ported from the reference, adapted)

### Per module (`LayersTests`, `DomainTests`)
- Domain must not depend on Application or Infrastructure.
- Application must not depend on Infrastructure.
- Value objects and domain events are immutable.
- Non-root entities have no public members.
- Entities do not reference other aggregate roots.

### Cross-module (`src/Tests/ArchTests/Modules/ModuleTests.cs`)
For **each** module M, types in M's Domain/Application/Infrastructure assemblies that are NOT
`INotificationHandler<>`, NOT `*IntegrationEventHandler`, and NOT the module's `Module`/startup type must have
**no dependency on any other module's namespace**.
This is the reference rule verbatim, and it is what makes "modules may reference only IntegrationEvents"
executable.

### API (`src/Tests/ArchTests/Api/ApiTests.cs`)
Each API endpoint area (`Weather.WebUI.Api.Modules.<Module>`) must not depend on any other module's namespace.

## Risk classification
| Scenario ID | Description | Risk |
|-------------|-------------|------|
| SC-e03s01-P0-01 | F→C conversion at freezing/boiling/negative/-40 | P0 |
| SC-e03s01-P0-02 | Dew point bounded by temperature | P0 |
| SC-e03s02-P0-01 | OWM payload maps to domain without leaking provider types | P0 |
| SC-e03s02-P0-02 | Upstream timeout / non-success → `WeatherProviderException` | P0 |
| SC-e03s04-P0-01 | Transient 5xx is retried up to `MaxRetryAttempts` then succeeds (call count = retries + 1) | P0 |
| SC-e03s04-P0-02 | Persistent 5xx exhausts retries → `WeatherProviderException` | P0 |
| SC-e03s04-P1-01 | `MaxRetryAttempts=0` disables retries (exactly one call) | P1 |
| SC-e03s04-P1-02 | 404 is never retried (exactly one call) | P1 |
| SC-e03s04-P1-03 | Transport `HttpRequestException` is retried | P1 |
| SC-e03s04-P2-01 | Invalid/inconsistent `OpenWeather:Resilience` values fail startup validation | P2 |
| SC-e03s03-P0-01 | GET weather returns the full metric set | P0 |
| SC-e03s03-P1-01 | Unknown city → 404 ProblemDetails | P1 |
| SC-e01s02-P0-01 | A module cannot depend on another module except IntegrationEvents | P0 |
| SC-e01s02-P0-02 | Domain has no dependency on Application/Infrastructure | P0 |
| SC-e02s03-P0-01 | Seeded city writes `CityAddedIntegrationEvent` to the outbox | P0 |
| SC-e04s02-P0-01 | Favorites handler upserts its City projection on the event | P0 |
| SC-e05s02-P0-01 | Notes handler upserts its City projection on the event | P0 |
| SC-e04s01-P0-01 | POST favorite persists in `FavoritesContext` and returns id | P0 |
| SC-e05s01-P0-01 | POST note persists in `NotesContext` and returns id | P0 |
| SC-e02s01-P1-01 | GET countries uses AsNoTracking | P1 |
| SC-e06s01-P1-01 | Country selection loads its cities | P1 |

## Offline strategy
- `IWeatherProvider` is a port; tests inject a stub. The concrete `OpenWeatherMapService` is tested with a canned
  `HttpMessageHandler` returning JSON fixtures — no sockets. Retry tests wire a counting stub as the primary
  handler through `AddWeatherModule` and assert HTTP call counts (never wall-clock); they set
  `RetryBaseDelaySeconds` to a sub-second value to stay fast.
- The Events Bus is in-memory and synchronous in tests; the background outbox processor runs inline.
- End-to-end tests replace real DI with per-module SQLite `:memory:` and the stub provider.
- A guard test asserts the real provider is never resolved in tests.

## Fixtures
- `WeatherHostFactory` — boots the host with stub provider + per-module `:memory:` contexts.
- `StubWeatherProvider`; `CannedOpenWeatherHandler`; `TestEventsBus`.
- Seed data: Countries GB/US/JP/AU/DE; 3–5 cities each.

## Definition of done (test)
- Every task has `verify:` and flips `failing → passing` only on exit 0.
- No test references a live URL.
- The cross-module isolation arch test fails if any module gains a forbidden dependency.
- Coverage: Domain and Application ≥ 90% line per module; overall ≥ 70% (soft target, reported).
