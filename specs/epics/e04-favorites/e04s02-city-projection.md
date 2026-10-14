# Story e04s02 — Favorites consumes CityAddedIntegrationEvent into a local projection

| Field | Value |
|-------|-------|
| ID | e04s02 |
| Epic | e04 Favorites Module (M3) |
| Status | todo |
| BCPs | 4 |
| Risk | P0 |
| Type | feat |

## 1. Summary
Handle `CityAddedIntegrationEvent` in the Favorites module and upsert a local `City` projection, so Favorites can
validate cities without ever reading Geography's tables.

## 2. Context
This is the consumer half of the integration-event pattern and the only way Favorites is allowed to know about
Geography.

## 3. User story
As the system, I want Favorites to keep its own city list, so the two modules stay decoupled.

## 4. Business value
Proves the modular-monolith integration model works end-to-end.

## 5. In scope
- `CityAddedIntegrationEventHandler` in the Favorites Application layer.
- `Cities.City` local projection entity + config (table `CityProjections`) in `FavoritesContext`.
- Idempotent upsert on receipt.
- `AddFavoriteCityValidator` uses the projection to reject unknown cities.

## 6. Out of scope
Other events; editing the projection manually.

## 7. Domain concepts
`City` (projection).

## 8. Layer touchpoints
| Assembly | Change |
|----------|--------|
| Favorites.Domain | `Cities/City` projection |
| Favorites.Application | `Cities/CityAddedIntegrationEventHandler` |
| Favorites.Infrastructure | `CityProjectionConfiguration` |
| Favorites.IntegrationEvents | references `Geography.IntegrationEvents` (allowed) |

## 9. API contract
None (async). Effect: unknown cities are rejected with `400`.

## 10. Dependencies
e02s03 (producer), e04s01 (module).

## 11. Risks
| Risk | Mitigation |
|------|-----------|
| Event delivered before subscriber registered | Deliver after startup; test runs inline |
| Duplicate delivery | Idempotent upsert |

## 12. Implementation steps
1. Add the `City` projection entity + config to `FavoritesContext` → verify: `dotnet test src/Modules/Favorites/Tests --filter Category=CityProjectionConfig`
2. Add `CityAddedIntegrationEventHandler` with idempotent upsert → verify: `dotnet test src/Modules/Favorites/Tests --filter Category=CityAddedHandler`
3. Reference `Geography.IntegrationEvents` only; confirm no other Geography reference → verify: `dotnet test src/Tests/ArchTests --filter Category=ModuleIsolation`
4. Use the projection in `AddFavoriteCityValidator` → verify: `dotnet test src/Modules/Favorites/Tests --filter Category=UnknownCity`
5. Integration: after seed events, a known city is accepted and an unknown one is rejected → verify: `dotnet test src/Tests/IntegrationTests --filter Category=FavoritesProjection`

## 13. Test plan
- Handler upserts; re-delivery is idempotent.
- Unknown city rejected.
- Arch test: only IntegrationEvents crosses.

## 14. Acceptance criteria (Gherkin)
```gherkin
Scenario: Projection updates on event
  Given a CityAddedIntegrationEvent for London/GB
  When the Favorites handler processes it
  Then FavoritesContext contains a London projection row

Scenario: Unknown city rejected
  Given no projection exists for "Nowhere"
  When I POST a favorite for Nowhere
  Then 400
```

## 15. Definition of done
Handler tests green; isolation arch test green.

## 16. Verification script
1. `dotnet test src/Modules/Favorites/Tests --filter Category=CityAddedHandler`.
2. `dotnet test src/Tests/ArchTests --filter Category=ModuleIsolation`.

## 17. Out of scope
Other integration events.

## 18. Risks / notes
If reference data never changes at runtime, seed the projection directly (documented alternative).

## 19. Sizing / BCP rationale
4 BCPs — event handler + projection + validation.

## 20. References
- `specs/adr/0005-modular-monolith-and-modules.md`
- `specs/adr/0006-per-module-data-isolation.md`
