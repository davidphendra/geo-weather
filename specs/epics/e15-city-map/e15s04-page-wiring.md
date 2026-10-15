# Story e15s04 — Page wiring + StationHeroCard coordinate switch

| Field | Value |
|-------|-------|
| ID | e15s04 |
| Epic | e15 City Map on the Weather Page (M2, core) |
| Status | planned |
| BCPs | 3 |
| Risk | P1 |
| Type | feat |
| Delta | MODIFIED |

## 1. Summary
Render `StationMap` in the Weather Explorer's left column directly beneath `ForecastStrip`, and switch
`StationHeroCard` to display the report's coordinates (falling back to the Geography `City` coordinates
when the report has none), so the hero card and the map share one source of truth (ADR-0018 D5, D8).

## 2. Context
`WeatherExplorer.razor` places `StationHeroCard` and `ForecastStrip` in `lg:col-span-7`, then
`CommandDispatchForm`/`ChangeTrackerResponseWell` in `lg:col-span-5`, then a full-width
`ArchitectureDeepDive`. `StationHeroCard` currently prints `City.Latitude`/`City.Longitude`. After e15s01
the report also carries coordinates.

## 3. User story
As a user, I want the map right under the forecast and the coordinates shown on the hero card to match the
map, so the page is consistent.

## 4. Business value
Completes the feature and removes the two-coordinate-source inconsistency.

## 5. In scope
- `WeatherExplorer.razor`: add `<StationMap Report="Model.Report"/>` under `ForecastStrip` in the left column.
- `StationHeroCard.razor`: prefer `Report.Latitude`/`Report.Longitude`, fall back to `City.Latitude`/`City.Longitude`.
- Integration test: `WeatherExplorerPageTests` asserts the "Surface Location Map" section renders.
- Update `specs/tech-architecture/UI_DESIGN_PLAN_LATEST.md` component inventory to include `StationMap`.

## 6. Out of scope
Component internals (e15s02); CSP (e15s03); coordinates parsing (e15s01); new metrics (the map is a
presentation concern).

## 7. Domain concepts
`WeatherReportModel` (client mirror), `CityModel` (fallback coordinates only).

## 8. Layer touchpoints
| Assembly | Change |
|----------|--------|
| Weather.WebUI | `Components/Pages/WeatherExplorer.razor`; `Components/Explorer/StationHeroCard.razor` |
| Weather.IntegrationTests | `Http/WeatherExplorerPageTests.cs` |
| specs/tech-architecture | `UI_DESIGN_PLAN_LATEST.md` |

## 9. API contract (consumed)
`GET /api/weather/{cityName}`.

## 10. Dependencies
e15s01, e15s02, e15s03.

## 11. Risks
| Risk | Mitigation |
|------|-----------|
| Hero card blanks when the report lacks coordinates | Explicit `City` fallback; covered by the page rendering test state |
| Map renders on pages other than `/` | `StationMap` is only referenced by `WeatherExplorer.razor` |
| Layout regression (map too small/large) | Fixed-height classes + `scripts/check-layout.sh`; manual viewport check |
| Two coordinate sources disagree | Hero card switched to the report source (single source of truth) |

## 12. Requirements (delta)
- **MODIFIED** — Weather Explorer composition.
  **Before:** left column = `StationHeroCard`, then `ForecastStrip`.
  **After:** left column = `StationHeroCard`, `ForecastStrip`, then `StationMap`.
- **MODIFIED** — `StationHeroCard` coordinate source.
  **Before:** `City.Latitude` / `City.Longitude`.
  **After:** `Report.Latitude` / `Report.Longitude`, falling back to `City` when absent.

## 13. Implementation steps
1. Add `<StationMap Report="Model.Report"/>` beneath `ForecastStrip` in `WeatherExplorer.razor` → verify: `dotnet test src/Tests/Weather.IntegrationTests --filter Category=WeatherExplorerPage`
2. Switch `StationHeroCard` to the report coordinates with a `City` fallback → verify: `dotnet test src/Tests/Weather.IntegrationTests --filter Category=WeatherExplorerPage`
3. Extend `WeatherExplorerPageTests` to assert the "Surface Location Map" heading renders; update the UI design component inventory → verify: `dotnet test src/Tests/Weather.IntegrationTests --filter Category=WeatherExplorerPage`
4. Story gate: full preflight plus layout and Tailwind guards → verify: `dotnet build Weather.sln --warnaserror && dotnet format Weather.sln --verify-no-changes && dotnet test Weather.sln && bash scripts/check-layout.sh && bash scripts/check-tailwind-css.sh`

## 14. Test plan
- Integration (`WeatherExplorerPage`): `GET /` returns 200 and contains "Surface Location Map"; the existing assertions (forecast heading, nav link) still pass.
- Manual: hero card coordinates equal the map's centre; selecting a city updates both.
- Regression: `/favorites` does not load Leaflet (no `unpkg` request).

## 15. Acceptance criteria (Gherkin)
```gherkin
Scenario: Map sits under the forecast
  Given a city is selected
  Then the left column shows the hero card, then the forecast strip, then the station map

Scenario: Hero card and map agree
  Given the report carries coordinates
  Then the hero card prints those coordinates
  And the map is centred on them

Scenario: Fallback
  Given the report has no coordinates but the city has them
  Then the hero card prints the city coordinates
  And the map section shows its unavailable state
```

## 16. Definition of done
Map wired under the forecast; hero card uses the report source with fallback; page test updated; Preflight,
layout, and Tailwind guards green.

## 17. Verification script
1. `dotnet run --project src/Weather.WebUI`.
2. Select a city; confirm the map appears under the forecast and the hero-card coordinates match.
3. Navigate to `/favorites`; confirm no Leaflet/tile requests in the network panel.

## 18. Risks / notes
`ArchitectureDeepDive` remains full width after the grid; the map stays inside the left column as agreed
(D5).

## 19. Sizing / BCP rationale
3 BCPs — page wiring + hero-card switch + integration test/doc update.

## 20. References
- `specs/adr/0018-city-map-leaflet.md` (D5, D8)
- `specs/tech-architecture/UI_DESIGN_PLAN_LATEST.md` (§5)
- `specs/epics/e14-forecast/e14s04-webui.md` (page-wiring precedent)
