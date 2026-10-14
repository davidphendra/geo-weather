# Story e05s02 — Notes consumes CityAddedIntegrationEvent into a local projection

| Field | Value |
|-------|-------|
| ID | e05s02 |
| Epic | e05 Notes Module (M4) |
| Status | done |
| BCPs | 4 |
| Risk | P0 |
| Type | feat |

## 1. Summary
Handle `CityAddedIntegrationEvent` in the Notes module and upsert a local `City` projection, so Notes validates
cities without reading Geography's tables.

## 2. Context
Mirrors e04s02; the second consumer proves the producer event is genuinely publish/subscribe.

## 3. User story
As the system, I want Notes to keep its own city list, so it stays decoupled from Geography.

## 4. Business value
Confirms multi-subscriber integration and per-module projections.

## 5. In scope
- `CityAddedIntegrationEventHandler` in Notes.Application.
- `Cities.City` projection + config (table `CityProjections`) in `NotesContext`.
- Idempotent upsert; `AddWeatherNoteValidator` rejects unknown cities.

## 6. Out of scope
Other events.

## 7. Domain concepts
`City` (projection).

## 8. Layer touchpoints
| Assembly | Change |
|----------|--------|
| Notes.Domain | `Cities/City` projection |
| Notes.Application | `Cities/CityAddedIntegrationEventHandler` |
| Notes.Infrastructure | `CityProjectionConfiguration` |
| Notes.IntegrationEvents | references `Geography.IntegrationEvents` (allowed) |

## 9. API contract
None (async). Effect: unknown cities → `404`.

## 10. Dependencies
e02s03, e05s01.

## 11. Risks
| Risk | Mitigation |
|------|-----------|
| Duplicate delivery | Idempotent upsert |
| Ordering | Upsert keyed by CityId |

## 12. Implementation steps
1. Add the `City` projection + config → verify: `dotnet test src/Modules/Notes/Tests --filter Category=CityProjectionConfig`
2. Add `CityAddedIntegrationEventHandler` with idempotent upsert → verify: `dotnet test src/Modules/Notes/Tests --filter Category=CityAddedHandler`
3. Confirm only `Geography.IntegrationEvents` crosses → verify: `dotnet test src/Tests/ArchTests --filter Category=ModuleIsolation`
4. Use the projection in `AddWeatherNoteValidator` → verify: `dotnet test src/Modules/Notes/Tests --filter Category=UnknownCity`
5. Integration: both Notes and Favorites projections update from the same event → verify: `dotnet test src/Tests/IntegrationTests --filter Category=MultiSubscriber`

## 13. Test plan
- Handler upserts; idempotent.
- Unknown city rejected.
- Multi-subscriber test: one event, two projections.

## 14. Acceptance criteria (Gherkin)
```gherkin
Scenario: Projection updates on event
  Given a CityAddedIntegrationEvent for London/GB
  Then NotesContext contains a London projection row

Scenario: Two subscribers
  Given a CityAddedIntegrationEvent
  Then both Favorites and Notes projections are updated
```

## 15. Definition of done
Handler + multi-subscriber tests green; isolation arch test green.

## 16. Verification script
1. `dotnet test src/Modules/Notes/Tests --filter Category=CityAddedHandler`.
2. `dotnet test src/Tests/IntegrationTests --filter Category=MultiSubscriber`.

## 17. Out of scope
Other events.

## 18. Risks / notes
Same pragmatic seeding alternative as e04s02.

## 19. Sizing / BCP rationale
4 BCPs — event handler + projection + validation.

## 20. References
- `specs/adr/0005-modular-monolith-and-modules.md`
