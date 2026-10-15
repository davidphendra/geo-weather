# Story e13s01 — Merge Favorites and Notes into the Journal module

| Field | Value |
|-------|-------|
| ID | e13s01 |
| Epic | e13 Merge Favorites and Notes into the Journal module (M3) |
| Status | done |
| BCPs | 12 |
| Risk | P0 |
| Type | refactor |

## 1. Summary
Merge the Favorites and Notes bounded contexts into one `Journal` module: one `JournalContext` over
`journal.db`, one `City` projection and event handler, one migration, two controllers, two retained contracts,
routes and telemetry instruments unchanged. Full analysis: `specs/IMPACT-merge-notes-into-favorites.md`.

## 2. Context
Favorites and Notes were near-clones (ADR-0016). ADR-0005 had rejected the merge but recorded the fallback;
`SCOPE_LATEST.yaml` carried it as an open decision. The user confirmed the merge and the nine decisions D1–D7.

## 3. User story
As a developer, I want one module owning saved cities and weather notes, so the duplication between Favorites
and Notes is removed while the public HTTP contract is preserved.

## 4. Business value
Fewer projects/files, one projection and event handler, simpler host composition — with no client-visible change.

## 5. In scope
- Rename the Favorites assembly set to `Weather.Modules.Journal.*`; absorb the Notes assembly set.
- One `JournalContext` (`journal.db`) owning `FavoriteCities`, `WeatherNotes`, `CityProjections`; rebaselined
  `CreateJournalSchema` migration.
- One `City` projection (with coordinate) and one `CityAddedIntegrationEventHandler`.
- Keep both contracts (`IFavoritesService`, `INotesModule`) and both controllers under `Api/Modules/Journal/`.
- Keep routes `POST/GET /api/cities/favorites`, `POST /api/weather/notes` byte-identical.
- Keep `notes.*` / `favorites.*` instruments; module tag becomes `Journal`.
- Retire `favorites.db`/`notes.db` (no data migration).
- Merge the Notes test project into the Journal test project; rewrite the multi-subscriber test.

## 6. Out of scope
New features; route changes; per-user data; data migration from the retired files.

## 7. Domain concepts
`FavoriteCity`, `WeatherNote`, `City` (projection).

## 8. Layer touchpoints
| Assembly | Change |
|----------|--------|
| `Journal.Domain` | renamed Favorites types + absorbed `WeatherNote`; one `JournalDomainException` |
| `Journal.Application` | absorbed Notes handlers/validators/DTO/repository; two contracts kept |
| `Journal.Infrastructure` | one `JournalContext`/migration/initializer/composition root |
| `Weather.WebUI` | `AddJournalModule`; health probes `JournalContext`; controllers relocated; single 404 arm |

## 9. API contract
Unchanged: `POST /api/cities/favorites` (201/409/404/...), `GET /api/cities/favorites` (200),
`POST /api/weather/notes` (201/422/404).

## 10. Dependencies
e04, e05, e09, e10, e11, e12 (all done).

## 11. Risks
| Risk | Mitigation |
|------|-----------|
| Contract drift | Byte-identical routes; e09/OpenAPI tests unchanged |
| Lost multi-subscriber proof | Accepted (D6); documented in ADR-0016; test rewritten |
| One-contract convention break | Recorded deviation; CONVENTIONS amended |
| Telemetry tag change | Instruments kept; module tag `Journal` documented |

## 12. Implementation steps
See `e13s01-tasks.yaml` (19 tasks across 8 phases).

## 13. Test plan
- Module tests: 38 (former Favorites `+ Notes`, minus duplicate projection/handler tests).
- Integration: routes, errors, projections, database configuration (`journal.db`).
- Arch: Journal isolation + observability boundary.
- Full suite: 301 tests green.

## 14. Acceptance criteria (Gherkin)
```gherkin
Scenario: Journal module owns both aggregates
  Given the JournalContext migration has run
  Then FavoriteCities, WeatherNotes and CityProjections are all addressable

Scenario: REST contract unchanged
  When I POST /api/weather/notes with a valid note
  Then the response is 201 with a Location header
  When I POST and GET /api/cities/favorites
  Then the responses are 201 and 200 as before

Scenario: Single projection from one event
  Given a seeded CityAddedIntegrationEvent for London/GB
  Then both a favorite and a note for London succeed
```

## 15. Definition of done
Build/format/tests green; no `Weather.Modules.Notes.*`/`Weather.Modules.Favorites.*` remain; ADR-0016 and the
architecture docs updated. No commits made.

## 16. Verification script
1. `dotnet build Weather.sln --warnaserror`.
2. `dotnet format Weather.sln --verify-no-changes`.
3. `dotnet test Weather.sln`.

## 17. Out of scope
Route changes; new UI; data migration.

## 18. Risks / notes
Two redundant Notes tests were removed (duplicate of merged Favorites coverage): 308 → 301 total tests.

## 19. Sizing / BCP rationale
12 BCPs — rename + merge of two 4-assembly modules, one migration, host wiring, tests, docs.

## 20. References
- `specs/adr/0016-merge-favorites-and-notes-into-journal.md`
- `specs/IMPACT-merge-notes-into-favorites.md`
- ADR-0005, ADR-0006, ADR-0010, ADR-0011
