# ADR-0006 — Per-module data isolation and schemas

**Status:** Proposed (needs user confirmation)
**Date:** 2025-10-13 (revised for the module-per-assembly layout)

## Context
Reference rule (MyMeetings README §3.1): "Each Module **has it's own data** in a separate schema - shared data
is not allowed. Module data could be moved into separate databases if desired." MyMeetings uses SQL Server with
one schema per module. This app uses SQLite (ADR-0002), which has no schemas.

## Decision
Because each module is now its own assembly set with its own composition root, data isolation is structural:

1. **One `DbContext` per persisting module**: `GeographyContext`, `FavoritesContext`, `NotesContext`.
   `Weather` persists nothing (external provider).
2. **One SQLite database file per module** in the running app:
   `data/geography.db`, `data/favorites.db`, `data/notes.db`. This is *stronger* than the reference's shared
   database with separate schemas — it is effectively data-isolation level 3 for SQLite.
3. **No cross-module foreign keys or joins.** A module reads only its own tables.
4. **A module may reference only another module's `IntegrationEvents` assembly**, never its `Domain`/
   `Application`/`Infrastructure` or its `DbContext`.
5. **Local projections for shared reference data.** Favorites and Notes keep their own `City` projection, kept
   current by `CityAddedIntegrationEvent`. They never query Geography's schema.
6. **Each module owns its migrations.**

### Upgrade path (documented, not built)
Move a module's file to a dedicated database server, or coalesce all modules onto SQL Server/PostgreSQL schemas
with `HasDefaultSchema("<module>")`. Contract and code do not change — only connection configuration.

## Rationale
- Matches the reference's intent (module data is private and separately movable).
- Removes the "shared database data" anti-pattern (REFERENCE.md §4: highest coupling) at the physical level.
- Keeps the extraction path open without any code change.

## Consequences
- Positive: data ownership is enforced by the connection string, not by convention.
- Positive: no cross-module query is even expressible — different contexts, different files.
- Negative: three files to create/migrate; three connection strings to configure.
- Negative: duplicated city data in Favorites/Notes (the deliberate price of decoupling; the projection is kept
  small — id, country code, name, coordinates).

## Cross-check with the audit rule G (extraction readiness)
Level is effectively 3 (separate database per module) on SQLite. Extraction risk is LOW; the only remaining step
would be moving a file to a server. Recorded as such in the eventual `MODULE_AUDIT_LATEST.md`.
