# Story e12s02 — Technical instrumentation (CQRS, events bus, provider, logs)

| Field | Value |
|-------|-------|
| ID | e12s02 |
| Epic | e12 Observability |
| Status | todo |
| BCPs | 5 |
| Risk | P1 |
| Type | feat |
| ADR | ADR-0014 |

## 1. Summary
Emit the shared technical instruments: one MediatR pipeline behavior for every CQRS request, events-bus
publish/handle signals, OpenWeatherMap provider signals, and `trace_id`/`span_id` log correlation.

## 2. Context
e12s01 provides the pipeline and the `WeatherMeters`/`WeatherActivity` primitives. The distro already covers
ASP.NET Core and HttpClient. Missing are per-request CQRS timing, events-bus visibility, and provider-specific
failure classification — all needed before business counters (e12s03) can be trusted.

## 3. User story
As an operator, I want every CQRS request, integration event, and upstream provider call to be measured and
traceable, so I can find the slow or failing step without reading source code.

## 4. Business value
Turns a failing HTTP metric into an actionable trace and a named sub-operation.

## 5. In scope
- `TelemetryBehavior<TRequest,TResponse>` in `Weather.BuildingBlocks.Application` (activity + `mediatr.requests`
  + `mediatr.request.duration`; tags: request name, kind query/command, module, outcome). Registered once in the
  host as an open-generic `IPipelineBehavior<,>`.
- `InMemoryEventsBus` instrumentation: `integration.event.published`, `.handled`, `.failed`, `.duration` +
  publish/handle activities.
- `CityCatalogEventPublisher` activity + published count on startup.
- `OpenWeatherMapService`: `weather.provider.requests`, `.failures{error.type}`, `.duration` + activity.
- Log enrichment with `trace_id`/`span_id`; JSON console formatter outside Development.
- Tests via `MeterListener`/`ActivityListener`.

## 6. Out of scope
- Business counters for weather/geography/favorites/notes (e12s03).
- `api.errors` (e12s03).
- EF Core instrumentation (ADR-0014 D3).

## 7. Domain concepts
None (cross-cutting instrumentation).

## 8. Layer touchpoints
| Assembly | Change |
|----------|--------|
| `Weather.BuildingBlocks.Application` | `Telemetry/TelemetryBehavior.cs`, `Telemetry/TelemetryRegistration.cs` |
| `Weather.BuildingBlocks.Infrastructure` | `Events/InMemoryEventsBus.cs` instrumentation |
| `Weather.Modules.Weather.Infrastructure` | `OpenWeatherMap/OpenWeatherMapService.cs` instrumentation |
| `Weather.Modules.Geography.Infrastructure` | `Events/CityCatalogEventPublisher.cs` instrumentation |
| `Weather.WebUI` | log enrichment + JSON console formatter; register the behavior |

## 9. API contract
Unchanged.

## 10. Dependencies
e12s01.

## 11. Risks
| Risk | Mitigation |
|------|-----------|
| Behavior double-registration across modules | register exactly once in the host (open generic applies to all MediatR requests) |
| Reflection tag values leak high cardinality | tags limited to request type name / module / kind / outcome (closed enums) |
| Instrumentation changes handler behavior or swallows exceptions | behavior records the outcome and **rethrows** the original exception unchanged |
| Duplicate spans with distro instrumentation | use `ActivitySource` and suppress only where a distro span already exists |
| Log enrichment breaks existing log assertions | enrichment is additive; existing tests assert messages, not format |

## 12. Implementation steps
1. Add `TelemetryBehavior<TRequest,TResponse>` + register once in the host → verify: `dotnet build Weather.sln --warnaserror`
2. Instrument `InMemoryEventsBus` (publish/handle/failure/duration + activities) → verify: `dotnet test src/BuildingBlocks/Tests/Weather.BuildingBlocks.Tests`
3. Instrument `CityCatalogEventPublisher` (span + count) → verify: `dotnet test src/Modules/Geography/Tests/Weather.Modules.Geography.Tests`
4. Instrument `OpenWeatherMapService` (provider requests/failures/duration + span) → verify: `dotnet test src/Modules/Weather/Tests/Weather.Modules.Weather.Tests`
5. Add `trace_id`/`span_id` to log scope; JSON console formatter outside Development → verify: `dotnet test Weather.sln`
6. Add `Category=Observability` assertions for mediatr/events/provider signals via `MeterListener` + `ActivityListener` → verify: `dotnet test Weather.sln --filter Category=Observability`
7. Full preflight → verify: `dotnet build Weather.sln --warnaserror && dotnet format Weather.sln --verify-no-changes && dotnet test Weather.sln`

## 13. Test plan
| Scenario | Level | Risk |
|----------|-------|------|
| `SC-e12s02-P1-01` A CQRS request increments `mediatr.requests` with name/kind/module/outcome | Unit | P1 |
| `SC-e12s02-P1-02` A failing handler still emits with outcome=error and rethrows | Unit | P1 |
| `SC-e12s02-P1-03` Publishing an integration event increments published + handled | Unit | P1 |
| `SC-e12s02-P1-04` A handler throwing increments `integration.event.failed` | Unit | P1 |
| `SC-e12s02-P1-05` A 404 from OWM increments `weather.provider.requests{status=404}` | Unit | P1 |
| `SC-e12s02-P1-06` A provider timeout increments `weather.provider.failures{error.type=timeout}` | Unit | P1 |

## 14. Acceptance criteria (Gherkin)
```gherkin
Scenario: CQRS request is timed
  Given telemetry is enabled in-process
  When a query handler runs
  Then mediatr.requests is incremented with the request name, kind, and module
  And mediatr.request.duration is recorded

Scenario: Handler failure is recorded and rethrown
  Given a handler throws WeatherNotFoundException
  When the request runs
  Then mediatr.requests records outcome=not_found and the exception still propagates

Scenario: Integration event is observable
  When CityAddedIntegrationEvent is published
  Then integration.event.published and integration.event.handled are incremented

Scenario: Upstream failure is classified
  When OpenWeatherMap times out
  Then weather.provider.failures{error.type=timeout} is incremented
```

## 15. Definition of done
Build, format, and tests green; technical instruments asserted with BCL listeners; no handler behaviour change.

## 16. Verification script
1. `dotnet test Weather.sln --filter Category=Observability`
2. `dotnet test src/BuildingBlocks/Tests/Weather.BuildingBlocks.Tests`
3. `dotnet test src/Modules/Weather/Tests/Weather.Modules.Weather.Tests`
4. Full preflight.

## 17. Out of scope
Section 6.

## 18. Risks / notes
The behavior must be exception-transparent: record, then rethrow. Never catch-and-swallow.

## 19. Sizing / BCP rationale
5 BCPs — behavior + registration, events bus, catalog publisher, provider, log correlation, tests.

## 20. References
- `specs/tech-architecture/OBSERVABILITY_PLAN_LATEST.md` §§5.1, 5.3, 6
- `specs/adr/0014-opentelemetry-observability.md`
