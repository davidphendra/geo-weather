# ADR-0010 — Favorites module decisions

**Status:** Proposed (needs user confirmation)
**Date:** 2025-10-13
**Related:** ADR-0005 (modules), ADR-0006 (data isolation), ADR-0008/0009 (self-contained modules, controllers)

## Context
Favorites (M3) is the first **write module** and the first **integration-event consumer**. It saves a city as a
favorite and exposes it through the API. Five decisions were settled during planning.

## Decisions

| # | Decision | Detail |
|---|----------|--------|
| F1 | **Event-driven city data** | Favorites upserts a local `City` projection from `CityAddedIntegrationEvent`. **Prerequisites:** the shared Outbox + In-memory Events Bus (`Weather.BuildingBlocks.Infrastructure`) and Geography publishing the event (`e02s03`, un-deferred). |
| F2 | **Keyed on `CityId`** | `FavoriteCities` references `CityId` (Guid, FK → `CityProjections`); unique index on `CityId`. Display name comes from a local join to `CityProjections`. |
| F3 | **Read endpoint included** | `GET /api/cities/favorites` lists favorites (joined to the projection), in addition to the required `POST`. |
| F4 | **Duplicate → `409 Conflict`** | Posting an existing favorite returns `409` with ProblemDetails. The unique index on `CityId` is the source of truth (concurrency-safe). |
| F5 | **Global favorites** | No users in scope ⇒ no `UserId`. Favorites are global. Introducing per-user favorites would require a real auth module, not a client-supplied id. |

## Rationale
- F1 makes the module's data ownership real (own projection) and exercises the async integration-event pattern the
  architecture is built on.
- F2 uses the stable identity the event already carries; keeps `FavoriteCities` normalized.
- F3 makes the feature demonstrable and shows the projection payoff.
- F4 gives an explicit, testable duplicate rule backed by a database constraint.
- F5 matches the requirement (no auth) and the standard "never trust a client-supplied identity".

## Consequences
- Positive: Favorites owns its data and is independently testable offline; the event pattern is proven end-to-end.
- Negative: F1 pulls forward the shared events infrastructure and `e02s03` — Favorites cannot ship before them.
- Negative: until `e02s03` is live, the projection is empty and every POST resolves to `404` (acceptable in a
  staged build; tests seed the projection directly).

## Prerequisites (must land first)
1. `e01s01` — `BuildingBlocks` projects exist.
2. `e01s02` — `InMemoryEventsBus` + per-module `Outbox` + background publisher.
3. `e02s03` — Geography raises a domain event on city registration and publishes `CityAddedIntegrationEvent`.

## Out of scope
Per-user favorites (needs auth); unfavorite/delete; favorite notes.
