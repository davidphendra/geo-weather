# Tech Stack

## Architectural style
**Modular monolith** sliced by bounded context, following `kgrzybek/modular-monolith-with-ddd`. Each module is
internally **Onion-layered** and uses **CQRS** (MediatR). One deployable unit.

| Rule | Enforcement |
|------|-------------|
| Module boundary = assembly boundary | Compiler |
| A module references another module ONLY via its `IntegrationEvents` assembly | `ModuleTests` (NetArchTest) — fails on any other namespace |
| Modules communicate only asynchronously via the Events Bus | `ModuleTests` excludes `*IntegrationEventHandler`; no other cross-module type may reference another module |
| Onion layering inside a module (Domain ← Application ← Infrastructure) | `LayersTests` per module |
| Each module owns its data/schema | One `DbContext` + one SQLite file per persisting module (ADR-0006) |

Module determination and rationale: `specs/adr/0005-modular-monolith-and-modules.md`. Access map:
`specs/tech-architecture/modules.map`.

## Bounded contexts (modules)

| Module | Kind | Own data | Context | Publishes | Consumes |
|--------|------|----------|---------|-----------|----------|
| **Geography** | Supporting | `geography.db` | `GeographyContext` | `CityAddedIntegrationEvent` | — |
| **Weather** | **Core** | none (external) | — | — | — |
| **Favorites** | Supporting | `favorites.db` | `FavoritesContext` | — | `CityAddedIntegrationEvent` |
| **Notes** | Supporting | `notes.db` | `NotesContext` | — | `CityAddedIntegrationEvent` |

Rejected contexts: UserAccess (no auth), Alerts/Notifications, Forecast/History. See ADR-0005.

## Project layout (`src/`)

```
BuildingBlocks/
  Weather.BuildingBlocks.Domain/          Entity, AggregateRoot, ValueObject, TypedIdValueBase,
                                          DomainEventBase, IDomainEvent, IBusinessRule,
                                          BusinessRuleValidationException, IAggregateRoot
  Weather.BuildingBlocks.Application/     ICommand, IQuery, CommandBase, QueryBase,
                                          Validation, processing pipeline, EventsBus abstractions
  Weather.BuildingBlocks.Infrastructure/  EF base config, Outbox, InMemoryEventsBus, UnitOfWork,
                                          Clock, domain-event dispatcher
Modules/
  Weather.Modules.Geography/
    Weather.Modules.Geography.Domain/          Countries/, Cities/, SharedKernel/
    Weather.Modules.Geography.Application/     Contracts/IGeographyService, Countries/, Cities/, Configuration/
    Weather.Modules.Geography.Infrastructure/  GeographyContext, GeographyService, Outbox/, Configuration/
    Weather.Modules.Geography.IntegrationEvents/ CityAddedIntegrationEvent
  Weather.Modules.Weather/
    ...Domain/        WeatherReports/ (WeatherReport, Temperature, DewPoint, WindInfo, ...)
    ...Application/   Contracts/IWeatherService, WeatherReports/GetCurrentWeather, Abstractions/IWeatherProvider
    ...Infrastructure/ OpenWeatherMapService, WeatherService
    ...IntegrationEvents/
  Weather.Modules.Favorites/
    ...Domain/        FavoriteCities/ (FavoriteCity), Cities/ (local City projection)
    ...Application/   Contracts/IFavoritesModule, FavoriteCities/AddFavoriteCity, Cities/
    ...Infrastructure/ FavoritesContext, FavoritesModule, Outbox/, Configuration/
    ...IntegrationEvents/ CityAddedIntegrationEventHandler (lives in Application/Infrastructure)
  Weather.Modules.Notes/  (mirror of Favorites for WeatherNote)
Weather.WebUI/          single host (ADR-0012); Components/ (Blazor UI) + Api/Modules/<Module>/*Controller.cs;
                        Program initializes modules

Tests (8 projects — 7 locked + controller unit tests):
  BuildingBlocks/Tests/Weather.BuildingBlocks.Tests/          BuildingBlocks unit tests
  Modules/<Module>/Tests/Weather.Modules.<Module>.Tests/      one per module (4); folders
                                                              UnitTests/ IntegrationTests/ ArchTests/
  Tests/Weather.ArchTests/                                    cross-module ModuleTests + ApiTests + BuildingBlocks guard
  Tests/Weather.IntegrationTests/                             end-to-end host tests (offline)
  Tests/Weather.WebUI.UnitTests/                             controller unit tests (ADR-0009)
```

> **Locked decision (2025-10-13):** 7 test projects — one per module (with `UnitTests`/
> `IntegrationTests`/`ArchTests` folders) plus `Weather.ArchTests`, `Weather.IntegrationTests`, and
> `Weather.BuildingBlocks.Tests`. The reference's three-test-projects-per-module shape (36 projects) is
> rejected for project-count economy; Grzybek's README explicitly permits merging. Test types stay
> separated by folder, so a later split into separate projects is mechanical.

### Per-module anatomy (the reference slice)
```
<Module>/
  Domain/            aggregates, value objects, domain events, business rules   (no framework deps)
  Application/       Contracts/I<Module>Service, feature folders (commands/queries), validators, Configuration
  Infrastructure/    <Module>Context (DbContext), <Module>Module (composition root), Outbox, Configuration
  IntegrationEvents/ the ONLY assembly other modules may reference
```

### Module API surfaces
| Module | Interface | Members |
|--------|-----------|---------|
| Geography | `IGeographyService` | `GetCountriesAsync`, `GetCitiesAsync` |
| Weather | `IWeatherService` | `GetCurrentWeatherAsync(cityName, countryCode, ct)` |
| Favorites | `IFavoritesModule` | `AddFavoriteAsync(cityName, countryCode, ct)` |
| Notes | `INotesModule` | `AddNoteAsync(cityName, countryCode, note, temperatureCelsius, ct)` |

## Cross-module integration (async only)

```
Geography ──CityAddedIntegrationEvent──► InMemoryEventsBus ──► Favorites.CityAddedIntegrationEventHandler
                                                            └► Notes.CityAddedIntegrationEventHandler
```
- Published from Geography's **Outbox** by the background processor; delivered after startup so subscribers exist.
- Favorites/Notes upsert a local `City` projection (id, countryCode, name, lat/lon).
- Weather is independent: it resolves the city through the provider (`q={city},{country}`).
- No direct cross-module method call anywhere.

## Runtime, framework, packages
| Concern | Choice | Notes |
|---------|--------|-------|
| Language | C# 14 | `nullable enable`, `ImplicitUsings enable` |
| Runtime | .NET 10 (SDK 10.0.401) | present |
| API | ASP.NET Core MVC controllers (`ControllerBase`, `[ApiController]`) | thin, per-module controllers; `ActionResult<T>` (ADR-0009) |
| Front-end | Blazor Web App | hosted in `Weather.WebUI` alongside the API (ADR-0012); consumes the API over HTTP |
| CQRS | MediatR 12.x | pinned — ADR-0004 |
| Validation | FluentValidation | pipeline behavior per module |
| ORM | EF Core 10 | one context per persisting module |
| Persistence | SQLite | one file per module under the configured `Database:DataDirectory` (default `App_Data`, outside `wwwroot`) — ADR-0006/0013 |
| Publish | runtime data excluded | `App_Data\**\*` is `CopyToOutputDirectory`/`CopyToPublishDirectory = Never`; `scripts/check-publish-no-data.sh` guards the artifact — ADR-0013 |
| Events | In-memory Events Bus + per-module Outbox | reference pattern |
| DI | Microsoft DI (per-module `Add<Module>Module()`) | deviation from the reference's Autofac — ADR-0005 |
| Arch tests | NetArchTest.Rules (or ArchUnitNET) | per-module + cross-module rules |
| API docs | `Microsoft.AspNetCore.OpenApi` | `/openapi/v1.json` |
| Observability | `Microsoft.OpenTelemetry` distro 1.1.0 → Azure Monitor; `OpenTelemetry.Instrumentation.Runtime` | ADR-0014; metrics/traces/logs; custom `Weather` meter + `Weather.Activity` source |

## Package decisions (slopcheck)
| Package | Tag | Rationale |
|---------|-----|-----------|
| `MediatR` 12.x | [OK] | Mature; Apache-2.0 avoids the v13 licensing change. Reference pins 12.2.0. |
| `FluentValidation` | [OK] | Reference uses 11.8.1; actively maintained. |
| `EF Core Sqlite` / `EF Core InMemory` | [OK] | First-party. |
| `NetArchTest.Rules` | [OK] | The reference's own arch-test tool; used to mirror its rules. |
| `NSubstitute` | [OK] | Reference uses it; actively maintained. |
| `Shouldly` | [OK] | Avoids FluentAssertions v8 commercial license (reference pins FA 6.12). |
| `Microsoft.AspNetCore.OpenApi` | [OK] | First-party. |
| `Microsoft.OpenTelemetry` 1.1.0 | [OK] | Microsoft OpenTelemetry Distro (GA) — one-call OTel with the Azure Monitor exporter, ASP.NET Core/HttpClient instrumentation, and resource detectors (ADR-0014). |
| `OpenTelemetry.Instrumentation.Runtime` | [OK] | Official OTel .NET; adds `dotnet.*` GC/thread-pool/process metrics the distro omits. |
| `Microsoft.Extensions.Http.Resilience` | [OK] | First-party resilience for the OpenWeatherMap client (timeout + bounded retry with jitter). |
| `xunit` + runner | [OK] | Reference uses NUnit; xUnit is our existing choice. |
| ~~Autofac / per-module IoC container~~ | [SUS] | Reference does it; catalog marks it contested (ADR-0005 deviation). Not used. |
| ~~FluentAssertions~~ | [SLOP] | v8 commercial license — avoid. |
| ~~External message broker~~ | [SLOP] | In-memory bus is sufficient in-process. |

## Known gray areas
- **Dew point** absent from OWM Current Weather 2.5 → derived via Magnus (ADR-0003).
- **Seeded reference data + integration events**: seed writes `CityAddedIntegrationEvent` to the outbox; the
  background processor publishes after startup. If reference data never changes at runtime, the projection may be
  seeded directly — a documented pragmatic alternative.
- **Module granularity**: 4 modules; the reference ships 5 for a smaller domain, so this is conservative.
- **Test granularity**: locked at 7 test projects (above).
