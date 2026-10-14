# Story e02s03 — Geography publishes CityAddedIntegrationEvent via the Outbox

> **DEFERRED.** This story is not part of the active Geography slice. It requires the shared Outbox +
> In-memory Events Bus and belongs with the write-side modules (Favorites/Notes) that consume the event.
> See `specs/adr/0008-geography-module-self-contained.md`. The spec below is retained for planning only.

| Field | Value |
|-------|-------|
| ID | e02s03 |
| Epic | e02 Geography Module (M1) |
| Status | todo |
| BCPs | 5 |
| Risk | P0 |
| Type | feat |

## 1. Summary
Make Geography publish a `CityAddedIntegrationEvent` (from its `IntegrationEvents` assembly) through its Outbox
whenever a city exists in the catalog, so Favorites and Notes can build local projections — the only legal
cross-module coupling.

## 2. Context
Reference rule: modules integrate asynchronously via the Events Bus; only `IntegrationEvents` crosses. This story
supplies the producer side; e04s02/e05s02 supply consumers.

## 3. User story
As the system, I want Geography to announce cities, so other modules stay decoupled from its schema.

## 4. Business value
Demonstrates the integration-event pattern the whole architecture rests on.

## 5. In scope
- `Geography.IntegrationEvents.CityAddedIntegrationEvent` (CityId, CountryCode, Name, Latitude, Longitude).
- Raising the event on the city catalog (seed writes it to the Outbox once).
- Outbox persistence + background publisher → EventsBus.
- Outbox tests.

## 6. Out of scope
Consumers (e04s02, e05s02); CityRemoved/CountryAdded.

## 7. Domain concepts
`City`, `CityAddedIntegrationEvent`.

## 8. Layer touchpoints
| Assembly | Change |
|----------|--------|
| Geography.IntegrationEvents | new event type |
| Geography.Domain | raise a domain event when a city is registered |
| Geography.Application | map domain event → integration event |
| Geography.Infrastructure | Outbox write + background publisher |

## 9. API contract
None (async).

## 10. Dependencies
e01s02 (EventsBus/Outbox), e02s02 (City).

## 11. Risks
| Risk | Mitigation |
|------|-----------|
| Seed events published before subscribers exist | Publish from the background processor after startup; test runs it inline |
| Duplicate delivery | Consumers upsert idempotently |

## 12. Implementation steps
1. Add `CityAddedIntegrationEvent` to the IntegrationEvents assembly → verify: `dotnet build Weather.sln --warnaserror`
2. Raise a domain event when a city is registered; map it to the integration event → verify: `dotnet test src/Modules/Geography/Tests --filter Category=CityAddedEvent`
3. Persist the event to the Outbox on `SaveChanges` → verify: `dotnet test src/Modules/Geography/Tests --filter Category=OutboxWrite`
4. Start the background publisher; event reaches the EventsBus → verify: `dotnet test src/Modules/Geography/Tests --filter Category=OutboxPublish`
5. Seed writes CityAdded events for seeded cities → verify: `dotnet test src/Modules/Geography/Tests --filter Category=SeedEvents`

## 13. Test plan
- Outbox write on SaveChanges.
- Publisher delivers to a test subscriber.
- Idempotent reprocessing.

## 14. Acceptance criteria (Gherkin)
```gherkin
Scenario: City registered raises an integration event
  Given a city is added to the Geography catalog
  When the unit of work commits
  Then a CityAddedIntegrationEvent row exists in the Outbox
  And the publisher delivers it to the EventsBus

Scenario: Seed publishes city events
  Given Geography is seeded with cities
  Then CityAddedIntegrationEvent is published for each seeded city
```

## 15. Definition of done
Outbox + publish tests green; event is the only Geography type referenced outside the module.

## 16. Verification script
1. `dotnet test src/Modules/Geography/Tests --filter Category=Outbox`.
2. `dotnet test src/Tests/ArchTests --filter Category=ModuleIsolation`.

## 17. Out of scope
External broker; CityRemoved.

## 18. Risks / notes
Pragmatic alternative: seed the projections directly if reference data never changes at runtime.

## 19. Sizing / BCP rationale
5 BCPs — event contract + outbox + publisher.

## 20. References
- `specs/adr/0005-modular-monolith-and-modules.md`
- `specs/adr/0006-per-module-data-isolation.md`
