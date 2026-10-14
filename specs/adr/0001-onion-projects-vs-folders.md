# ADR-0001 — Onion layout: separate projects vs folders

**Status:** SUPERSEDED by ADR-0005 (module-per-assembly). The decision below kept four shared layer projects and
placed modules inside them; ADR-0005 replaces that with per-module assembly sets, which preserves Onion layering
*within each module* and makes the module boundary an assembly boundary.
**Date:** 2025-10-13

## Context
The requirement says Onion Architecture and parenthetically allows "folder instead of project".
The Domain layer must have "zero external framework dependencies" and layer boundaries must hold.

## Decision
Use **separate class-library projects** under `src/`:
`Weather.Domain`, `Weather.Application`, `Weather.Infrastructure`, `Weather.Api`, `Weather.WebUI`.

> **SUPERSEDED by ADR-0005.** The four shared layer projects are replaced by per-module assembly sets
> (`Weather.Modules.<Module>.{Domain,Application,Infrastructure,IntegrationEvents}`) plus `BuildingBlocks`.
> Onion dependency direction is preserved inside each module. Keep this ADR for history only.

## Rationale
- Compile-time boundary enforcement: a forbidden `using` fails the build, not just review.
- `Weather.Domain` genuinely references the BCL only — the "zero framework dependencies" rule becomes structural.
- Cheaper reviews: the dependency graph is visible in the solution, enforced at build time.
- The existing `Weather.WebUI` project is already a separate project, so a mixed layout would be inconsistent.

## Consequences
- Positive: boundaries are executable; architecture tests are a backstop, not the only guard.
- Negative: more projects and more `.csproj` files than a folder-only layout.
- Negative: e01s01 must move `Weather.WebUI` under `src/` and update `Weather.sln`.

## Alternatives rejected
- **Folders in one project:** simpler, but `Weather.Domain` would transitively reference ASP.NET Core and EF Core, violating the explicit "zero external framework dependencies" requirement.
- **Module-per-assembly (Grzybek):** see ADR-0005 — a contested .NET tactic that competes with the mandated layer projects.
