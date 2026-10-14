# Story e04s02 — Favorites consumes CityAddedIntegrationEvent into a local projection

| Field | Value |
|-------|-------|
| ID | e04s02 |
| Epic | e04 Favorites Module (M3) |
| Status | todo |
| BCPs | 3 |
| Risk | P0 |
| Type | feat |

## 1. Summary
Subscribe to `CityAddedIntegrationEvent` and upsert the Favorites-local `City` projection, so `AddFavoriteCity`
can resolve cities without ever reading Geography's data.

## 2. Context
The consumer half of the integration-event pattern (ADR-0010 F1). Its prerequisites — the shared Outbox + In-memory
Events Bus and Geography's `e02s03` — must be live before this story yields data.

## 3. User story
As the system, I want Favorites to keep its own city list, so the two modules stay decoupled.

## 4. Business value
Proves asynchronous module integration and keeps data ownership real.

## 5. In scope
- Reference `Geography.IntegrationEvents` from `Favorites.Application` — the **only** cross-module reference.
- `CityAddedIntegrationEventHandler` (in `Favorites.Application/Cities`) that upserts `CityProjections` idempotently.
- Subscription wired via the shared Events Bus.
- Architecture isolation test (`ModuleIsolation`): only `IntegrationEvents` crosses.

## 6. Out of scope
Publishing events; other Geography events; event schema versioning.

## 7. Domain concepts
`City` (projection).

## 8. Layer touchpoints
| Assembly | Change |
|----------|--------|
| `Favorites.Application` | `Cities/CityAddedIntegrationEventHandler`; references `Geography.IntegrationEvents` |
| `Favorites.Infrastructure` | event subscription registration in `AddFavoritesModule` |
| `Favorites.IntegrationEvents` | (empty; consumer only) |

## 9. API contract
None (async). Effect: `POST /api/cities/favorites` can resolve cities → 201 instead of 404.

## 10. Dependencies
e04s01; prerequisites e01s01/e01s02 and e02s03.

## 11. Risks
| Risk | Mitigation |
|------|-----------|
| Event delivered before subscriber registered | bus delivers after startup (outbox → background publisher) |
| Duplicate delivery | idempotent upsert keyed by `CityId` |
| Cross-module leak | `ModuleIsolation` arch test forbids anything but `IntegrationEvents` |

## 12. Implementation steps
1. Reference `Geography.IntegrationEvents` from Favorites.Application → verify: `dotnet build …Favorites.Application.csproj --warnaserror`
2. Add `CityAddedIntegrationEventHandler` with idempotent upsert → verify: `dotnet test src/Modules/Favorites/Tests/Weather.Modules.Favorites.Tests --filter Category=CityAddedHandler`
3. Wire the subscription in `AddFavoritesModule` → verify: `dotnet build …Favorites.Infrastructure.csproj --warnaserror`
4. Assert isolation (only IntegrationEvents crosses) → verify: `dotnet test src/Tests/Weather.ArchTests --filter Category=ModuleIsolation`
5. Integration: a `CityAddedIntegrationEvent` populates `CityProjections` → verify: `dotnet test src/Tests/Weather.IntegrationTests --filter Category=FavoritesProjection`
6. Standards gate → verify: `dotnet format …Favorites.Infrastructure.csproj --verify-no-changes`

## 13. Test plan
- Unit: handler upserts a projection row; re-delivery is idempotent.
- Arch: only `Geography.IntegrationEvents` is referenced.
- Integration: publishing the event fills the projection and enables `POST` 201.

## 14. Acceptance criteria (Gherkin)
```gherkin
Scenario: Projection updates on event
  Given a CityAddedIntegrationEvent for London/GB
  When the Favorites handler processes it
  Then FavoritesContext contains a London projection row

Scenario: Cross-module reach is limited to integration events
  Given the Favorites assemblies
  Then they reference only Geography.IntegrationEvents from Geography
```

## 15. Definition of done
Handler + idempotency tests green; isolation arch test green; projection populated in integration.

## 16. Verification script
1. `dotnet test src/Modules/Favorites/Tests/Weather.Modules.Favorites.Tests --filter Category=CityAddedHandler`.
2. `dotnet test src/Tests/Weather.ArchTests --filter Category=ModuleIsolation`.

## 17. Out of scope
Publishing; event versioning.

## 18. Risks / notes
Without e02s03 the projection stays empty; tests seed/publish the event directly.

## 19. Sizing / BCP rationale
3 BCPs — event handler + projection upsert + isolation test.

## 20. References
- `specs/adr/0010-favorites-module-decisions.md`
- `specs/adr/0005-modular-monolith-and-modules.md`
