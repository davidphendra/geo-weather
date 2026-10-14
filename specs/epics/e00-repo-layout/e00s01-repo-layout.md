# Story e00s01 — Scaffold the modular-monolith repository layout

> **Amended by e10s01 (ADR-0012, 2025-10-15):** `Weather.Api` was merged into `Weather.WebUI`. The layout and
> reference graph below are the original e00 as-built record; the current single host is `Weather.WebUI`.

| Field | Value |
|-------|-------|
| ID | e00s01 |
| Epic | e00 Repository Layout Scaffolding |
| Status | done |
| BCPs | 8 |
| Risk | P1 |
| Type | chore |
| Scope | LAYOUT ONLY — no feature code |

## 1. Summary
Create the complete `kgrzybek/modular-monolith-with-ddd`-style repository skeleton: root build config, the three
`BuildingBlocks` projects, the four module assembly sets (Domain/Application/Infrastructure/IntegrationEvents),
the thin `Weather.Api` host, the `Weather.WebUI` relocation, all test projects, the project-reference graph, and
the `Weather.sln` wiring. Every project must compile empty.

## 2. Context
The plan slices the repo by bounded context. Before any BuildingBlocks type, DbContext, handler, or endpoint is
written (e01+), the physical layout and its reference rules must exist and compile. This story delivers exactly
that and nothing more.

## 3. User story
As a developer, I want the full repo skeleton in place and building, so that later stories add types into an
already-correct structure instead of restructuring it.

## 4. Business value
De-risks every later story: boundaries are physical (assembly-per-module) and the solution builds from day one.

## 5. In scope (layout only)
- Root: `global.json`, `Directory.Build.props`, `Directory.Packages.props`, `.editorconfig`.
- `src/BuildingBlocks/Weather.BuildingBlocks.{Domain,Application,Infrastructure}` (empty).
- `src/Modules/<M>/Weather.Modules.<M>.{Domain,Application,Infrastructure,IntegrationEvents}` for
  `<M>` ∈ {Geography, Weather, Favorites, Notes} (empty).
- `src/Weather.Api` (minimal host project; a bare `Program.cs` only).
- `src/Weather.WebUI` (existing project moved under `src/`).
- Test projects (**7 — locked**): `src/Modules/<M>/Tests/Weather.Modules.<M>.Tests` (×4, folders
  UnitTests/IntegrationTests/ArchTests), `src/Tests/Weather.ArchTests`, `src/Tests/Weather.IntegrationTests`,
  `src/BuildingBlocks/Tests/Weather.BuildingBlocks.Tests`. The reference's 3-test-projects-per-module shape
  (36 projects) is rejected for project-count economy; test types stay separated by folder.
- Project-reference graph exactly as in §8.
- All projects added to `Weather.sln`.
- One placeholder smoke test per test project so `dotnet test` is meaningful.
- A layout guard that forbids any cross-module project reference except `*.IntegrationEvents`.

## 6. Out of scope (must NOT be created here)
- Any BuildingBlocks type (Entity, AggregateRoot, ICommand, EventsBus, Outbox…).
- Any module contract (`I<Module>Service`), DbContext, aggregate, handler, validator, or endpoint.
- Any `appsettings` content beyond the WebUI defaults.
- Tests that assert behaviour.
- Git operations (no init, no commit).

## 7. Domain concepts
None.

## 8. Project-reference graph (the layout's load-bearing rule)

```
Weather.BuildingBlocks.Domain              → (none)
Weather.BuildingBlocks.Application         → BuildingBlocks.Domain
Weather.BuildingBlocks.Infrastructure       → BuildingBlocks.Application, BuildingBlocks.Domain

Weather.Modules.<M>.Domain                 → BuildingBlocks.Domain
Weather.Modules.<M>.Application            → <M>.Domain, BuildingBlocks.Application
Weather.Modules.<M>.Infrastructure         → <M>.Application, <M>.Domain, BuildingBlocks.Infrastructure
Weather.Modules.<M>.IntegrationEvents      → BuildingBlocks.Domain

Weather.Modules.Favorites.Application      → Weather.Modules.Geography.IntegrationEvents   (ONLY cross-module ref)
Weather.Modules.Notes.Application          → Weather.Modules.Geography.IntegrationEvents   (ONLY cross-module ref)
Weather.Modules.Weather.*                  → (no cross-module ref)

Weather.Api                                → each module's Application + Infrastructure, BuildingBlocks.Infrastructure
Weather.WebUI                              → (none — HTTP contract only)

Weather.Modules.<M>.Tests                  → <M>.Domain, <M>.Application, <M>.Infrastructure, <M>.IntegrationEvents
Weather.ArchTests                          → all module assemblies + Weather.Api + BuildingBlocks
Weather.IntegrationTests                   → Weather.Api + all module Infrastructure
Weather.BuildingBlocks.Tests               → BuildingBlocks.Domain, Application, Infrastructure
```

A cross-module reference to anything other than `*.IntegrationEvents` is a layout defect.

## 9. API contract
None. The host has a bare `Program.cs`; no endpoints.

## 10. Dependencies
None (first epic).

## 11. Risks
| Risk | Mitigation |
|------|-----------|
| `TreatWarningsAsErrors` breaks empty projects | Keep `Directory.Build.props` to the agreed flags; build gate catches it |
| Central Package Management conflicts with the existing WebUI csproj | Enable CPM in `Directory.Packages.props`; the WebUI has no packages |
| Accidentally adding feature code | §6 out-of-scope is a hard boundary; review the diff |
| Project-reference graph drifts | Layout guard task + `ModuleTests` in e07 |
| Test projects with zero tests | One placeholder smoke test per project |

## 12. Implementation steps
1. Add root build config → verify: `dotnet build Weather.sln --warnaserror`
2. Create the three BuildingBlocks projects + references → verify: `dotnet build src/BuildingBlocks/Weather.BuildingBlocks.Infrastructure --warnaserror`
3. Create the Geography module assembly set + references → verify: `dotnet build src/Modules/Geography/Weather.Modules.Geography.Infrastructure/Weather.Modules.Geography.Infrastructure.csproj --warnaserror`
4. Create the Weather module assembly set + references → verify: `dotnet build src/Modules/Weather/Weather.Modules.Weather.Infrastructure/Weather.Modules.Weather.Infrastructure.csproj --warnaserror`
5. Create the Favorites module assembly set + reference `Geography.IntegrationEvents` → verify: `dotnet build src/Modules/Favorites/Weather.Modules.Favorites.Infrastructure/Weather.Modules.Favorites.Infrastructure.csproj --warnaserror`
6. Create the Notes module assembly set + reference `Geography.IntegrationEvents` → verify: `dotnet build src/Modules/Notes/Weather.Modules.Notes.Infrastructure/Weather.Modules.Notes.Infrastructure.csproj --warnaserror`
7. Create the `Weather.Api` host project + references → verify: `dotnet build src/Weather.Api/Weather.Api.csproj --warnaserror`
8. Move `Weather.WebUI` under `src/` and fix its references → verify: `dotnet build src/Weather.WebUI/Weather.WebUI.csproj --warnaserror`
9. Create all test projects + references + one smoke test each → verify: `dotnet test Weather.sln`
10. Add every project to `Weather.sln` → verify: `dotnet build Weather.sln --warnaserror`
11. Layout guard: no cross-module project reference except `IntegrationEvents` → verify: `bash scripts/check-layout.sh` (script created in step 11) or the equivalent grep
12. Full layout gate → verify: `dotnet build Weather.sln --warnaserror && dotnet format Weather.sln --verify-no-changes && dotnet test Weather.sln`

## 13. Test plan
- One placeholder smoke test per test project (`true`), so the harness runs and `dotnet test` is green.
- No behavioural tests.

## 14. Acceptance criteria (Gherkin)
```gherkin
Scenario: The whole skeleton builds
  Given the repository skeleton
  When I run dotnet build Weather.sln --warnaserror
  Then the build succeeds

Scenario: Every project is in the solution
  Given the expected project inventory
  When I run dotnet sln Weather.sln list
  Then every expected project appears

Scenario: Module boundaries are physical
  Given the module project files
  Then no module references another module's Domain, Application, or Infrastructure
  And only Favorites/Notes reference Geography.IntegrationEvents

Scenario: The test harness runs
  Given the placeholder smoke tests
  When I run dotnet test Weather.sln
  Then all tests pass

Scenario: No feature code leaked in
  Given the layout diff
  Then BuildingBlocks and modules contain no domain types, DbContexts, handlers, or endpoints
```

## 15. Definition of done
- `dotnet build Weather.sln --warnaserror`, `dotnet format --verify-no-changes`, and `dotnet test Weather.sln` all pass.
- The project inventory in §5 exists; the reference graph in §8 holds.
- The diff contains no feature code.

## 16. Verification script (step-by-step)
1. `dotnet build Weather.sln --warnaserror` → succeeds.
2. `dotnet sln Weather.sln list` → 28 projects as in §5.
3. `dotnet test Weather.sln` → placeholder tests pass.
4. `git status --porcelain` (when a repo exists) → only project/config files, no types.

## 17. Out of scope
Everything in §6.

## 18. Risks / notes
Canonical test-project paths are defined here; later stories' `verify:` paths must match them.

## 19. Sizing / BCP rationale
8 BCPs — broad and mechanical (28 projects + config + reference graph), low logic.

## 20. References
- `specs/adr/0005-modular-monolith-and-modules.md`
- `specs/adr/0006-per-module-data-isolation.md`
- `specs/tech-architecture/tech-stack.md`
