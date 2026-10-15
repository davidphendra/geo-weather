# Impact & Plan — Merge the Notes and Favorites modules into `Journal`

> **Status:** decisions CONFIRMED (see §11). **No code written. No commits.**
> Deliverable of `assess-impact` (full mode). The repo's existing `specs/IMPACT_LATEST.md` documents the
> unrelated e10 "merge API into WebUI" change and has intentionally **not** been overwritten.

### Confirmed decisions at a glance

| # | Decision | Choice |
|---|----------|--------|
| D1 | Target module identity | **Rename both modules to `Journal`** (`Weather.Modules.Journal.*`, `JournalContext`) |
| D2 | Persistence | **One `JournalContext` → `journal.db`** (FavoriteCities + WeatherNotes + CityProjections, one migration) |
| D2b | Existing local data | **Retire `favorites.db` + `notes.db`; no data migration** |
| D3 | HTTP contract | **Routes byte-identical** (`/api/cities/favorites`, `/api/weather/notes`) |
| D3b | Controllers | **Two controller classes** under `Api/Modules/Journal/` |
| D4 | Public contract | **Keep two interfaces** (`IFavoritesService` + `INotesModule`) — documented deviation from the one-contract rule |
| D5 | Telemetry | **Keep `notes.*` + `favorites.*` instruments**; accept `module=Journal`, `handler.module=Weather.Modules.Journal.Infrastructure` |
| D6 | Multi-subscriber proof | **Accept the loss**; `MultiSubscriber` test rewritten as single-consumer |
| D7 | Migration style | **Staged**, one green Preflight per stage |

---

## 1. Target

Merge the **Notes** bounded context (`Weather.Modules.Journal.*`) into the **Favorites** bounded context
(`Weather.Modules.Journal.*`) and **rename the result to `Journal`** (`Weather.Modules.Journal.*`), producing
**one module** that owns both `FavoriteCity` and `WeatherNote`, one `City` projection, one composition root, and
one `DbContext`. Both modules are renamed; this is a merge **and** a rename.

The user's stated reason — "both of them are related to each other" — is factually supported: the two modules
today are near-clones (see §2). This is a real, pre-identified option in the repo (see §3), not a novel idea.

---

## 2. How the two modules look today

Favorites and Notes are structurally almost identical. The duplication the merge would remove:

| Concern | Favorites | Notes | Merge effect |
|---|---|---|---|
| Assemblies | Domain/Application/Infrastructure/IntegrationEvents (4) | same (4) | −4 projects |
| DbContext + SQLite file | `FavoritesContext` / `favorites.db` | `NotesContext` / `notes.db` | one context / one file (decision D2) |
| `City` projection | has `GeoCoordinate` | no coordinate | one projection (superset, coordinate kept) |
| `CityAddedIntegrationEventHandler` | full copy | full copy (minus coordinate) | one handler |
| `ICityProjectionRepository` | `FindByNameAndCountryAsync` + `UpsertAsync` | identical | one interface |
| `IUnitOfWork` | own copy | own copy | one interface |
| Public contract | `IFavoritesService` | `INotesModule` (outlier name) | **both kept** (D4=B) |
| `CityNotFoundException` | own type | own type (separate!) | one type |
| Telemetry | `favorites.operations` / `.duration` / `.saved.count` | `notes.operations` / `.duration` / `.saved.count` | keep or merge (decision D5) |
| Test project | 20 tests | 20 tests | one project with ~40 tests |

The **only** genuine difference is the payload each module persists (`FavoriteCity` keyed on `CityId` vs
`WeatherNote` denormalized with note text/temperature). That is aggregate-level, not bounded-context-level.

---

## 3. Precedent already in the repo (read this before deciding)

The merge has been considered twice and deliberately **deferred, not settled**:

1. **`specs/adr/0005-modular-monolith-and-modules.md`** explicitly names the alternative:
   > *"Considered alternative: merge Favorites + Notes into one `WeatherJournal` context. Rejected: different
   > aggregates (`FavoriteCity` vs `WeatherNote`), different invariants, different lifecycles — Grzybek keeps
   > `Registrations` and `UserAccess` separate on the same reasoning. Fallback documented."*

2. **`specs/product/SCOPE_LATEST.yaml`** lists it as an **open** `decisions_to_confirm`:
   > *"Module set: four bounded contexts — Geography, Weather, Favorites, Notes (recommended) vs merging
   > Favorites+Notes into one WeatherJournal context."*

So this is a **reversal of a recorded decision**, and it must be recorded as such (new ADR amending ADR-0005,
plus a SCOPE update). It is not just a refactor.

**Third consequence — the cost nobody sees immediately:** `e05s02`'s stated value is *"the second consumer
proves the producer event is genuinely publish/subscribe"* and the `MultiSubscriber` integration test exists
solely to prove one `CityAddedIntegrationEvent` fills *two* projections. After the merge there is exactly **one**
consumer of `CityAddedIntegrationEvent`, so that architectural demonstration disappears. If that proof matters,
the merge trades it away. (This is the strongest counter-argument and must be an explicit yes/no.)

---

## 4. Dependents — code (`src`, non-generated)

**Inside `src/Modules/Notes/` (42 files, all moved/renamed/deleted):** the whole assembly set — Domain, Application
(handlers, validators, repositories, contracts, telemetry), Infrastructure (context, config, migrations, module
extension), empty `IntegrationEvents` assembly, and the 10-file test project.

**Outside the Notes module (32 files reference `Notes`/`notes`):**

| File | Nature of dependency |
|---|---|
| `src/Weather.WebUI/Hosting/ModuleRegistrationExtensions.cs` | `using ...Notes.Infrastructure; services.AddNotesModule(...)` |
| `src/Weather.WebUI/Hosting/HealthEndpoints.cs` | resolves `JournalContext` for `/health/ready` |
| `src/Weather.WebUI/Api/Modules/Notes/NotesController.cs` + `AddNoteRequest.cs` | injects `INotesModule`, `[Route("api/weather/notes")]` |
| `src/Weather.WebUI/Api/ErrorHandling/GlobalExceptionHandler.cs` | maps **both** `Favorites...CityNotFoundException` and `global::Notes...CityNotFoundException` |
| `src/Weather.WebUI/Services/WeatherApiClient.cs` | `AddNoteAsync` → `api/weather/notes` (route can stay) |
| `src/Weather.WebUI/Services/CommandDispatchModel.cs`, `WeatherNoteModel.cs`, `EntityFilterModel.cs` | comments + type names |
| `src/Weather.WebUI/Components/Pages/Favorites.razor`, `Components/Favorites/*`, `AppNav.razor`, `Explorer/CommandDispatchForm.razor` | already a single "Favorites & Saved Notes" surface; comments/labels |
| `src/Weather.WebUI/appsettings.json` | `Database:Files.Notes = notes.db`; connection-string composition in `Program.cs`/`ModuleDatabaseExtensions` |
| `src/BuildingBlocks/.../Telemetry/WeatherMeters.cs` | `notes.operations`, `notes.duration`, `Tags.NotesOutcome` instruments |
| `Weather.sln` | 5 project entries (Notes.Domain/Application/Infrastructure/IntegrationEvents + Notes.Tests) + nesting |
| `src/Weather.WebUI/Weather.WebUI.csproj` | two `ProjectReference`s to Notes.Application/Infrastructure |

**Telemetry identity (subtle, cross-cutting):** `TelemetryBehavior.ResolveModule` derives the `module` metric tag
from the namespace segment after `Modules`; `InMemoryEventsBus` tags `handler.module` with the **assembly name**.
Today note commands emit `module=Notes` and the projection handler emits `handler.module=Weather.Modules.Journal.Infrastructure`.
After a straight fold into `Weather.Modules.Journal.*`, both silently change to `Favorites` /
`Weather.Modules.Journal.Infrastructure`. Any workbook/alert filtering on the old values breaks (decision D5).

---

## 5. Dependents — tests

| Test asset | Tests | Impact |
|---|---|---|
| `src/Modules/Notes/Tests/**` (10 files) | 20 `[Fact]/[Theory]` | moved into `Weather.Modules.Journal.Tests`; namespaces updated |
| `src/Modules/Favorites/Tests/**` (11 files) | 20 | receives the merged tests |
| `src/Tests/Weather.IntegrationTests/Http/NotesEndpointTests.cs` | `Category=Notes` | routes survive; host/context name changes |
| `src/Tests/Weather.IntegrationTests/Http/NotesErrorsTests.cs` | `Category=Notes`, `NotesValidation` | resolves `JournalContext` directly |
| `src/Tests/Weather.IntegrationTests/Http/NotesProjectionTests.cs` | `Category=MultiSubscriber` | **semantics change** — becomes a single-consumer test |
| `src/Tests/Weather.IntegrationTests/Http/FavoritesProjectionTests.cs` | `Category=FavoritesProjection` | merges with the above |
| `src/Tests/Weather.IntegrationTests/Http/DatabaseConfigurationTests.cs` | `Category=DatabaseConfiguration` | asserts `notes.db` exists → must change |
| `src/Tests/Weather.IntegrationTests/Http/OpenApiContractTests.cs` | contract | should stay green if routes are unchanged |
| `src/Tests/Weather.IntegrationTests/Support/WeatherHostFactory.cs`, `MissingApiKeyHostFactory.cs` | harness | `ConnectionStrings:Notes` removed |
| `src/Tests/Weather.IntegrationTests/Weather.IntegrationTests.csproj` | build | remove Notes.Infrastructure ref |
| `src/Tests/Weather.ArchTests/ModuleIsolationTests.cs` | `Category=ModuleIsolation` | `NotesReferencesOnlyGeographyIntegrationEvents` rewritten for the merged module |
| `src/Tests/Weather.ArchTests/ObservabilityBoundaryTests.cs` | `Category=Observability` | remove `NotesDomainException` + `INotesModule`; add merged module |
| `src/Tests/Weather.ArchTests/Weather.ArchTests.csproj` | build | remove 4 Notes project refs |
| `src/Tests/Weather.WebUI.UnitTests/Controllers/NotesControllerTests.cs` | `Category=NotesController` | injects `INotesModule`; namespace change |
| `src/Tests/Weather.WebUI.UnitTests/Services/CommandDispatchModelTests.cs` | note-endpoint test | endpoint string unchanged; type names may change |

Test-count baseline: `specs/state.yaml` records a green Preflight at **308 tests** (current branch
`feature/e03s04-resilience-config`). Merging must end green at a comparable count (Notes' 20 tests are relocated,
not discarded).

---

## 6. Dependents — specs, docs, ops (largest surface)

**≈89 files** under `specs/ docs/ ops/` mention Notes (51 of them under `specs/epics/`). Grouped:

- **Decisions / product:** `adr/0005`, `adr/0006`, `adr/0008`, `adr/0010`, `adr/0013`, `adr/0014`;
  `product/SCOPE_LATEST.yaml`, `product/GLOSSARY_LATEST.yaml`, `product/VISION_LATEST.yaml`.
- **Architecture:** `tech-architecture/modules.map`, `tech-stack.md`, `TEST_PLAN_LATEST.md`,
  `SECURITY_PLAN_LATEST.md`, `OBSERVABILITY_PLAN_LATEST.md`, `DESIGN_PLAN_LATEST.md`,
  `UI_DESIGN_PLAN_LATEST.md`, `AZURE_PIPELINE_PLAN_LATEST.md`.
- **Plan/state:** `release-plan.yaml` (modules M3/M4 + epics e04/e05), `state.yaml` (architecture modules,
  `async_edges` M1→M3/M1→M4), `execution-status.yaml` (e05s01/e05s02), `PLAN_LATEST.md`, `REFACTOR_LATEST.md`.
- **Epic capsules:** `epics/e04-favorites/**`, `epics/e05-notes/**`, plus cross-refs in e00, e01, e02, e03,
  e06s04, e07, e08, e09, e10, e11, e12.
- **Ops:** `docs/operations/observability.md`, `ops/azure-monitor/workbook.json` (queries filter
  `favorites.operations`/`notes.operations`/`notes.saved.count`).
- **Verifications:** `specs/verifications/e06s04.md` and others referencing notes.
- **Repo instructions:** `AGENTS.md`, `CONVENTIONS.md` — module table, boundary rules, "permitted cross-module
  flows: Geography → Favorites and Geography → Notes".

This documentation surface is the dominant cost of the change — larger than the code.

---

## 7. Affected stories / epics

| Story | Status | Effect |
|---|---|---|
| **e04s01 / e04s02** (Favorites) | done | absorbed aggregate + projection; still valid |
| **e05s01 / e05s02** (Notes) | epic.yaml `done`; `execution-status.yaml` says `todo` (drift) | become sub-stories of the merged module; e05s02's multi-subscriber AC is invalidated |
| **e06s04** (Favorites & Saved Notes page) | done | unaffected functionally (UI is already unified); comments/D3 wording updated |
| **e07s01** (arch tests) | todo | isolation/observability test wiring changes |
| **e08s01** (docs/README/module map) | todo | module list changes |
| **e12s03** (business metrics) | done | instrument ownership + module tag labels |
| **SCOPE S4/S5**, release-plan `modules` M3/M4 | — | one module + one epic instead of two |

Note the pre-existing status drift (`e05s01/s02` marked `done` in the capsule but `todo` in
`execution-status.yaml`). Whichever way the merge goes, reconcile this.

---

## 8. Test coverage and gaps

**Coverage that protects the merge:** endpoint tests (201/409/404/422), error → ProblemDetails tests, projection
tests, database-configuration tests, controller tests, and arch tests all exist and are the regression net.

**Gaps (new tests required):**
1. No test asserts that the **single merged `DbContext`** stores both `FavoriteCities` and `WeatherNotes` and
   that `CityProjections` is written once.
2. No test asserts the **REST surface is byte-identical** after the move (`POST /api/weather/notes`,
   `POST /api/cities/favorites`, `GET /api/cities/favorites`, status codes, `Location`, ProblemDetails `code`).
3. No test asserts **`/health/ready`** still reports ready after removing `JournalContext`.
4. The `MultiSubscriber` test's replacement: after merge there is one consumer — the test must be rewritten or
   deleted, and the decision recorded (it cannot keep its current meaning).
5. If `notes.db` is retired: no test covers migration/handling of pre-existing `notes.db` data.

---

## 9. Risk

**Risk: High.**

Rationale: it changes a **shared architectural boundary** (module set, data ownership, composition root),
touches ~32 code files outside the target, ~89 spec/doc files, `Weather.sln`, telemetry labels, and a database
schema/file — while the e09 REST contract and module-isolation guarantees must survive unchanged. The
`assess-impact` lightweight score is well above the gate (high fan-in, high fan-out, recent module churn across
two committed features), so a `grill-me`/decision session is mandatory **before** `plan-work`.

---

## 10. Benefits vs costs

**Benefits**
- Removes genuine duplication: one `City` projection, one event handler, one repository pair, one `DbContext`,
  one `IUnitOfWork`, one `CityNotFoundException`, one composition root.
- Matches the ubiquitous reality: the UI is already one "Favorites & Saved Notes" workspace.
  *(Note: with D4=B the merged module keeps two public contracts, so it does **not** gain the CONVENTIONS
  "exactly one `I<Module>Service`" property; that deviation is recorded rather than realised.)*
- Fewer projects (−4 src, −1 test) and one fewer SQLite file to migrate/health-check.

**Costs / risks**
- Reverses the recorded ADR-0005 decision; needs a new ADR + SCOPE update (governance, not just code).
- Destroys the multi-subscriber integration-event demonstration (`e05s02`), reducing the architectural story.
- One module with two aggregates and two invariants/lifecycles is arguably a weaker bounded-context boundary —
  the exact argument ADR-0005 used. This is a **modelling** judgement, not a mechanical one.
- Large documentation churn; `Weather.sln` surgery; possible telemetry/workbook breakage; a data-file decision.

---

## 11. Confirmed decisions (resolved with the user, one by one)

| # | Decision | Choice | Consequence captured in the plan |
|---|---|---|---|
| **D1** | Target module identity | **Rename both modules to `Journal`** (`Weather.Modules.Journal.*`, `JournalContext`) | Both modules are renamed, so BOTH sides move; ~89 spec/doc refs + `Weather.sln` + telemetry labels change. |
| **D2** | Persistence | **One `JournalContext` → `journal.db`** | One migration, one health probe, one connection string; `Database:Files`/`DatabaseConfigurationTests` updated. |
| **D2b** | Existing local data | **Retire `favorites.db` + `notes.db`; no data migration** | Recorded in ADR-0016; local `.db` files are gitignored/ignored. |
| **D3** | HTTP contract | **Routes byte-identical** | No change to e09/ADR-0011 contract, `WeatherApiClient`, or UI. |
| **D3b** | Controllers | **Two controller classes** under `Api/Modules/Journal/` | Routes + `[ProducesResponseType]` unchanged; controller tests move namespace. |
| **D4** | Public contract | **Keep two interfaces** (`IFavoritesService` + `INotesModule`) | **Deviation from CONVENTIONS' one-contract rule** — must be recorded in ADR-0016 and CONVENTIONS amended. |
| **D5** | Telemetry | **Keep `notes.*` + `favorites.*` instruments**; `module=Journal` | Update `OBSERVABILITY_PLAN_LATEST.md`, `docs/operations/observability.md`, `workbook.json`. |
| **D6** | Multi-subscriber proof | **Accept the loss** | `MultiSubscriber` test rewritten as single-consumer; trade-off documented in ADR-0016. |
| **D7** | Migration style | **Staged**, one green Preflight per stage | Sequence in §13. |

---

## 12. Confirmed merge shape (`Journal`)

```
Weather.Modules.Journal.Domain
  Cities/City (with GeoCoordinate)
  FavoriteCities/{FavoriteCity, FavoriteCityId}
  Notes/{WeatherNote, WeatherNoteId}
  JournalDomainException             (absorbs FavoritesDomainException + NotesDomainException)

Weather.Modules.Journal.Application
  Abstractions/{ICityProjectionRepository, IFavoriteCityRepository,
                IFavoriteCityReadRepository, IWeatherNoteRepository, IUnitOfWork, Exceptions}
  Cities/CityAddedIntegrationEventHandler        (one handler)
  FavoriteCities/AddFavoriteCity, FavoriteCities/GetFavorites
  Notes/AddWeatherNote + WeatherNoteDto
  Contracts/{IFavoritesService, INotesModule}    (two contracts kept — D4=B)
  Telemetry/{FavoritesMetrics, NotesMetrics}

Weather.Modules.Journal.Infrastructure
  JournalContext (DbSet<FavoriteCity>, DbSet<WeatherNote>, DbSet<City>) → journal.db
  Configurations/{FavoriteCity, WeatherNote, CityProjection, GeoCoordinateConverter}
  Database/JournalDatabaseInitializer (sets both saved-count gauges)
  Migrations (rebaselined CreateJournalSchema)
  Repositories/{FavoriteCityRepository, FavoriteCityReadRepository, WeatherNoteRepository, CityProjectionRepository}
  FavoritesService (IFavoritesService), JournalModule (INotesModule), JournalModuleExtensions
  DesignTime/JournalContextFactory

Weather.Modules.Journal.IntegrationEvents        (kept, empty)
Weather.Modules.Favorites.* / Weather.Modules.Notes.*  (removed)
```

Host: `AddJournalModule` only; `HealthEndpoints` probes `GeographyContext` + `JournalContext`; two controllers
under `Api/Modules/Journal/`; `GlobalExceptionHandler` keeps a single `CityNotFoundException` arm;
`appsettings.json` `Database:Files.Journal = journal.db`.

---

## 13. Staged plan (D7=A; decisions confirmed — implementation NOT started)

**Phase 0 — Governance & spec (no code)**
1. Write **ADR-0016 "Merge Favorites and Notes into the `Journal` module"**, amending ADR-0005 (module set +
   rename) and ADR-0006 (one schema). Record: the rejected keep-separate option, the multi-subscriber
   trade-off (D6), the retire-old-files decision (D2b), and the **D4 deviation** from the one-contract rule.
2. Update SCOPE (S4/S5 + resolve `decisions_to_confirm`), GLOSSARY (Favorites/Notes/Journal/City projection),
   CONVENTIONS module table + permitted flows + contract rule (deviation), `modules.map`, `release-plan.yaml`,
   `state.yaml`, `execution-status.yaml`, `AGENTS.md`.
   → verify: docs consistent; no code touched.

**Phase 1 — Baseline & guard**
3. Run Preflight on the current branch and record the green baseline (build/format/tests).
4. Rewrite `ModuleIsolationTests` to express the `Journal` module boundary rule.
   → verify: `dotnet test src/Tests/Weather.ArchTests --filter Category=ModuleIsolation`.

**Phase 2 — Rename Favorites → Journal (mechanical, behaviour-preserving)**
5. Rename the 4 Favorites assemblies/folders/namespaces to `Weather.Modules.Journal.*`; rename
   `JournalContext`→`JournalContext`, `FavoritesModuleExtensions`→`JournalModuleExtensions`, and the
   Favorites domain exception. Update `Weather.sln` + all `ProjectReference`s.
   → verify: `dotnet restore Weather.sln && dotnet build Weather.sln --warnaserror` + Favorites tests.

**Phase 3 — Absorb Notes (behaviour-preserving)**
6. Move `WeatherNote`/`WeatherNoteId` into `Journal.Domain/Notes`; fold `NotesDomainException` into
   `JournalDomainException`.
7. Collapse to one `City` projection + one `CityAddedIntegrationEventHandler` (keep the coordinate).
8. Move Notes application types into `Journal.Application`; **keep `IFavoritesService` and `INotesModule` as
   two contracts (D4=B)**; drop the duplicate `CityNotFoundException` in favour of `Journal`'s single type.
9. Add `WeatherNotes` DbSet + config to `JournalContext`; rebaseline the migration as `CreateJournalSchema`;
   move `WeatherNoteRepository`; update `JournalDatabaseInitializer` (both saved-count gauges); remove
   `JournalContext`.
10. Register everything in `AddJournalModule`; remove `AddNotesModule`.
    → verify after each: `dotnet build <project> --warnaserror` + targeted `dotnet test --filter Category=...`
    (`WeatherNote`, `AddNoteValidator`, `AddNoteHandler`, `NotesModule`, `CityAddedHandler`, `FavoritesMigrations`).

**Phase 4 — Host wiring**
11. `ModuleRegistrationExtensions`: `AddJournalModule` only. `HealthEndpoints`: probe `JournalContext` (drop
    `JournalContext`). `Program.cs`/`ModuleDatabaseExtensions` + `appsettings.json`: `journal.db`; remove the
    Notes connection string/file. `GlobalExceptionHandler`: single `CityNotFoundException` arm. Relocate both
    controllers to `Api/Modules/Journal/` (routes unchanged).
    → verify: `dotnet build src/Weather.WebUI/Weather.WebUI.csproj --warnaserror`, `curl /health/ready`.

**Phase 5 — Tests**
12. Merge `Weather.Modules.Journal.Tests` into `Weather.Modules.Journal.Tests` (namespaces + assembly removal).
13. Update `Weather.ArchTests` (observability + isolation + csproj).
14. Update `Weather.IntegrationTests` (Notes* → Journal, `FavoritesProjection`, `DatabaseConfiguration` now
    asserts `journal.db`, host factories, csproj); rewrite the `MultiSubscriber` test as single-consumer (D6).
15. Update `Weather.WebUI.UnitTests/Controllers/NotesControllerTests.cs`.
16. Add the new gap tests (§8 items 1–3).
    → verify: `dotnet test Weather.sln`.

**Phase 6 — Solution & build cleanup**
17. `Weather.sln`: remove `Weather.Modules.Journal.*` (4) + `Notes.Tests`; remove Notes/Favorites
    `ProjectReference`s from WebUI/ArchTests/IntegrationTests.
    → verify: `dotnet restore Weather.sln && dotnet build Weather.sln --warnaserror`.

**Phase 7 — Docs, telemetry, ops**
18. Update `OBSERVABILITY_PLAN_LATEST.md`, `docs/operations/observability.md`, `ops/azure-monitor/workbook.json`
    for the module-tag change (instruments kept). Sweep the remaining `specs/`/`docs/` references.

**Phase 8 — Verify**
19. Full Preflight + manual smoke (`POST /api/weather/notes`, `POST /api/cities/favorites`,
    `GET /api/cities/favorites`, `/health/ready`, `/favorites` page). Write `specs/verifications/eNNsNN.md`.

---

## 14. Definition of done

- One module assembly set (`Weather.Modules.Journal.*`); `Weather.Modules.Journal.*` and
  `Weather.Modules.Journal.*` gone from `Weather.sln`.
- One `JournalContext` owning `FavoriteCities`, `WeatherNotes`, `CityProjections`; one `journal.db`; health
  check probes it.
- REST surface byte-identical; e09 contract tests and OpenAPI tests green.
- Architecture tests pass for the merged module (isolation + layer + observability boundary).
- `dotnet build Weather.sln --warnaserror`, `dotnet format Weather.sln --verify-no-changes`,
  `dotnet test Weather.sln` all green; verification evidence recorded.
- ADR-0016 + SCOPE/GLOSSARY/CONVENTIONS/modules.map updated; no dangling `Favorites`/`Notes` module references
  except the intentionally kept `IFavoritesService`/`INotesModule` contract names and the `notes.*`/`favorites.*`
  instruments.
- No commits until explicitly requested.

---

## 15. Recommended action

**Decisions are locked. Next: write ADR-0016 and the Phase-0 spec updates, then create the epic capsule and
run `plan-work`.** Do not implement from this document directly (AGENTS.md workflow mandate). Implementation
proceeds in the staged order in §13, one green Preflight per stage, without committing.
