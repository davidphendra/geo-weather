# Story e06s04 — Favorites & Saved Notes page

| Field | Value |
|-------|-------|
| ID | e06s04 |
| Epic | e06 WebUI Front-End |
| Status | todo |
| BCPs | 7 |
| Risk | P1 |
| Type | feat |

## 1. Summary
Build the `/favorites` page to match `specs/designs/favorites_saved_notes_ef_core_state/code.html`: metrics strip,
control bar, entity table, quick-add command form, CQRS feed, pipeline topology, and the raw JSON inspector modal.

## 2. Context
`GET /api/cities/favorites` is the only list the backend exposes; the Notes module is POST-only. The page keeps
the prototype's layout but renders favorites as the entity rows and shows **Not available** for note rows/counts
(D3). No fabricated rows.

## 3. User story
As a user, I want to view and manage my saved favorite cities, and add a new favorite or note from one place.

## 4. Business value
The write-side surface and the second designed page.

## 5. In scope
- `Favorites.razor` (`@page "/favorites"`, `@rendermode InteractiveServer`), 8/4 grid.
- `MetricsStrip`/`MetricsCard` (tracked entities, favorites count, notes = `—`, context, last mutation from real loads).
- `EntityControlBar` (search, country filter, `All | Favorites` segmented control, New Command).
- `EntityTable`/`EntityRow`/`TypeBadge`/`SnapshotCell` — favorites rows; note column shows **Not available**.
- `QuickAddCommandForm` (Favorite default, Note toggle) + real `201` feedback.
- `CqrsMutationFeed` logging real requests; `PipelineTopologyCard` (static).
- `RawJsonInspectorModal` (focus trap, `Esc` to close) rendering the real DTO JSON.

## 6. Out of scope
Notes list / notes GET (separate epic); server-side paging (single page); soft-delete (no API).

## 7. Domain concepts
`FavoriteCityModel`, `CityModel`, `WeatherNoteModel`.

## 8. Layer touchpoints
| Assembly | Change |
|----------|--------|
| Weather.WebUI | `Components/Pages/Favorites.razor`, `Components/Favorites/*` |

## 9. API contract (consumed)
`GET /api/cities/favorites`; `POST /api/cities/favorites`; `POST /api/weather/notes`; `GET /api/countries` (filter).

## 10. Dependencies
e06s01, e06s02, e04s01.

## 11. Risks
| Risk | Mitigation |
|------|-----------|
| Design implies a notes list that does not exist | D3: render "Not available", count `—`, `All | Favorites` only |
| Table shows unknown columns (snapshot/tags) | Render from real fields; unavailable cells explicit |
| Modal traps focus / breaks keyboard | Focus trap + `Esc` + restore focus (Q12) |
| Delete action has no endpoint | Omit delete, or mark as unavailable |

## 12. Implementation steps
1. Build `MetricsStrip`/`MetricsCard` derived from real loads → verify: `dotnet build src/Weather.WebUI/Weather.WebUI.csproj --warnaserror`
2. Build `EntityControlBar` (search, country, `All | Favorites`) → verify: `dotnet test src/Tests/Weather.WebUI.UnitTests --filter Category=EntityFilter`
3. Build `EntityTable`/`EntityRow`/`TypeBadge`/`SnapshotCell` with D3 unavailable cells → verify: `dotnet build src/Weather.WebUI/Weather.WebUI.csproj --warnaserror`
4. Build `QuickAddCommandForm` (Favorite/Note toggle) → verify: `dotnet test src/Tests/Weather.WebUI.UnitTests --filter Category=CommandDispatch`
5. Build `CqrsMutationFeed` + `PipelineTopologyCard` → verify: `dotnet build src/Weather.WebUI/Weather.WebUI.csproj --warnaserror`
6. Build `RawJsonInspectorModal` with keyboard support → verify: `dotnet build src/Weather.WebUI/Weather.WebUI.csproj --warnaserror`
7. Wire `Favorites.razor` and add an integration test for GET /favorites → verify: `dotnet test src/Tests/Weather.IntegrationTests --filter Category=FavoritesPage`

## 13. Test plan
- Unit: filter model (search/country/type) returns the expected subset.
- Unit: quick-add maps Favorite → favorites endpoint and Note → notes endpoint.
- Integration: `GET /favorites` returns the shell markers.
- Manual: add a favorite → it appears in the table and the feed logs a `201`.

## 14. Acceptance criteria (Gherkin)
```gherkin
Scenario: Favorites are listed
  Given the API returns two favorites
  When I open "/favorites"
  Then the entity table shows two rows and the favorites count is 2

Scenario: Notes are honest
  Given the backend has no notes list
  Then note-related cells show "Not available" and the type filter offers only All and Favorites

Scenario: Add a favorite
  Given valid city/country input
  When I dispatch the Favorite command
  Then the feed logs HTTP 201 and the count increments
```

## 15. Definition of done
`/favorites` matches the prototype layout/styles; real data only; filters + modal work; Preflight green.

## 16. Verification script
1. `dotnet run --project src/Weather.WebUI`.
2. Open `/favorites`; confirm metrics, table, filters, and modal.
3. Add a favorite; confirm the row, count, and feed update.

## 17. Out of scope
Note editing/listing; paging; export (download) unless trivially client-side.

## 18. Risks / notes
"Tracked Entities" and "Last Mutation" are derived from real loads + the last client request; label them so they
are not read as server-side counters.

## 19. Sizing / BCP rationale
7 BCPs — one dense page, a filter model, a form, and a modal.

## 20. References
- `specs/tech-architecture/UI_DESIGN_PLAN_LATEST.md` §5, §6, §12 (D3/Q4/Q6/Q12)
- `specs/designs/favorites_saved_notes_ef_core_state/code.html`
