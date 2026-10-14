# ADR-0008 — Geography module is self-contained (read side first)

**Status:** Proposed (needs user confirmation)
**Date:** 2025-10-13

## Context
Geography (M1) is the first module that persists data. The reference architecture expects shared
`BuildingBlocks` (`Entity`, `AggregateRoot`, `ValueObject`, `ICommand`/`IQuery`, `ValidationBehavior`,
`Outbox`, `InMemoryEventsBus`). We deferred BuildingBlocks when implementing Weather (ADR-0007). The
decision now is whether Geography introduces BuildingBlocks, repeats the self-contained approach, or ships
only the read side.

## Decision
For this slice, Geography is **self-contained and read-only**:

1. **No BuildingBlocks.** Domain uses plain sealed types with guard clauses; CQRS uses MediatR `IRequest<T>`
   directly with a **module-local `ValidationBehavior`**.
2. **EF Core lives only in the module's Infrastructure** (`GeographyContext`, SQLite, `geography.db`),
   own migration stream — no shared persistence base.
3. **Read side only.** `GET /api/countries` and `GET /api/countries/{countryCode}/cities`, both
   `AsNoTracking()`.
4. **Filtering happens in the database.** Read repositories expose intent-specific, filtered queries; there is
   **no `GetAllAsync`-style method**. Predicates (`Where`), ordering, and projection are applied on `IQueryable`
   so EF Core translates them to SQL — never load-then-filter in memory. `GET /api/countries` accepts an
   optional `?search=` that becomes a SQL `LIKE`.
4. **`CityAddedIntegrationEvent` is deferred** (e02s03). It requires the shared Outbox + In-memory Events
   Bus, which is out of scope for a Geography-only slice and belongs with the write-side modules
   (Favorites/Notes) that consume it.

## Rationale
- Keeps the slice to one module; no cross-cutting infrastructure is introduced.
- Consistent with the Weather module's self-contained precedent, so the codebase stays uniform.
- The read capability is independently shippable and testable offline.

## Consequences
- Positive: Geography read side ships without a BuildingBlocks prerequisite.
- Positive: EF Core + seeding + `AsNoTracking` are exercised end-to-end.
- Negative: the `ValidationBehavior` is duplicated per module until BuildingBlocks lands; the two are
  reconciled when the shared kernel is introduced.
- Negative: `CityAddedIntegrationEvent` cannot be published yet, so Favorites/Notes projections are blocked
  until the event infrastructure exists.

## Locked refinements (planning)
| # | Decision |
|---|----------|
| C1 | **Catalog scope:** full ISO-3166 country list (~250) committed offline, with a handful of major cities each. |
| C2 | **City identity:** typed `CityId` over a deterministic `Guid` (stable, name-independent). |
| C3 | **Schema:** EF Core migrations (`Countries`, `Cities`) + a design-time factory; `Migrate()` on module init. |
| C4 | **Seed:** catalog data applied by EF Core **migration scripts** (SQL), not a runtime seeder. Tests call `Migrate()` so the seed applies. |
| C5 | **Search:** `GET /api/countries?search=` → SQL `Name LIKE`; plain `GET /api/countries` still returns all. |
| C6 | **Naming:** the Geography module's public API is **`IGeographyService`** (implementation **`GeographyService`**), not `IGeographyModule`. The composition-root extension stays `AddGeographyModule`. |

## Alternatives rejected
- **Introduce BuildingBlocks now:** out of scope for a Geography-only slice; needs its own epic.
- **Module-local outbox/event bus:** would duplicate shared infrastructure that Favorites/Notes also need
  and complicate a later extraction.
