# ADR-0016 — Merge Favorites and Notes into the `Journal` module

**Status:** Accepted
**Date:** 2025-10-15
**Amends:** ADR-0005 (module set), ADR-0006 (per-module data isolation)
**Related:** ADR-0010 (Favorites decisions), ADR-0011 (response contract), ADR-0013 (App_Data), ADR-0014 (observability)

## Context

The app shipped two bounded contexts — **Favorites** (`FavoriteCity` aggregate) and **Notes**
(`WeatherNote` aggregate) — as full assembly sets with their own `DbContext` and SQLite file. In practice the
two were near-clones:

- identical `City` projection (Favorites additionally stored `GeoCoordinate`),
- identical `CityAddedIntegrationEventHandler`, `ICityProjectionRepository`, `IUnitOfWork`,
- two separate `CityNotFoundException` types,
- one UI surface ("Favorites & Saved Notes") and one quick-add form that toggles between the two mutations.

ADR-0005 considered merging them into a `WeatherJournal` context and rejected it, but recorded the option as a
documented fallback. `specs/product/SCOPE_LATEST.yaml` carried it as an open `decisions_to_confirm`. The team
has now chosen to merge.

## Decision

1. **Merge** Favorites and Notes into a single module **`Journal`** (`Weather.Modules.Journal.*`), keeping four
   assemblies (Domain / Application / Infrastructure / IntegrationEvents).
2. **One schema:** a single `JournalContext` over one SQLite file `journal.db`, owning `FavoriteCities`,
   `WeatherNotes`, and `CityProjections`. One rebaselined migration `CreateJournalSchema`.
3. **One `City` projection** (with `GeoCoordinate`) and **one** `CityAddedIntegrationEventHandler`.
4. **HTTP contract unchanged:** `POST/GET /api/cities/favorites` and `POST /api/weather/notes` keep their URLs,
   status codes, `Location`, and ProblemDetails `code`s (ADR-0011 / epic e09).
5. **Two controller classes** (`FavoritesController`, `NotesController`) relocated to
   `Weather.WebUI/Api/Modules/Journal/`, each injecting its module contract.
6. **Two public contracts retained:** `IFavoritesService` and `INotesModule`, both in
   `Journal.Application.Contracts`.
7. **Telemetry instruments retained:** `notes.*` and `favorites.*` families stay (dashboards/alerts survive).
   The auto-derived `module` tag becomes `Journal` and `handler.module` becomes
   `Weather.Modules.Journal.Infrastructure`.
8. **Local data:** `favorites.db` and `notes.db` are retired; no data migration. `journal.db` is created from
   migrations on first run (files are gitignored; assessment/greenfield app).

## Rationale

- Removes real duplication (projection, handler, repositories, unit of work, context, composition root).
- Aligns code with the ubiquitous language shown in the UI: one place for "saved cities and observations".
- Reduces project count (4 src + 1 test removed) and SQLite files from three to two.

## Consequences

**Positive**
- One event handler and one projection to maintain; no cross-context drift.
- Simpler host composition and health probe (`GeographyContext` + `JournalContext`).
- Fewer projects and one fewer database file.

**Negative / accepted trade-offs**
- Reverses the ADR-0005 modelling position; the module now has two aggregates with different invariants and
  lifecycles.
- **Loses the multi-subscriber demonstration.** Previously Favorites and Notes were two independent consumers of
  `CityAddedIntegrationEvent`, which proved true publish/subscribe (story e05s02, `MultiSubscriber` test).
  Journal is now the sole consumer. The Events Bus still supports fan-out; it is simply no longer exercised.
  The `MultiSubscriber` test was rewritten as a single-consumer projection test
  (`JournalProjectionTests`).
- Two public contracts remain in one module, so the CONVENTIONS "exactly one `I<Module>Service`" rule is not
  achieved by this merge (see deviation below).

## Deviations

- **One-contract rule.** CONVENTIONS.md states each module exposes exactly one `I<Module>Service`. Journal keeps
  two (`IFavoritesService`, `INotesModule`). CONVENTIONS.md is amended to permit "one contract per bounded
  aggregate / API area" and this merge is recorded as the first instance.

## Alternatives considered

- **Keep two modules (status quo).** Rejected: leaves the duplication and the split that the UI already hides.
- **Merge but keep two city projections** (to preserve the multi-subscriber test). Rejected: re-introduces the
  duplication the merge exists to remove; fan-out can be re-exercised when a genuine second consumer appears.
- **Rename the merged module to `WeatherJournal`.** Dropped in favour of the shorter `Journal` namespace.

## Prerequisites satisfied

The merge was implemented after Favorites and Notes were both `done`, their tests green, and the API response
contract (e09) and single host (e10) in place.
