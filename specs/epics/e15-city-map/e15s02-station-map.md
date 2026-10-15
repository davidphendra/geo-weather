# Story e15s02 — StationMap component + lazy Leaflet CDN + OpenStreetMap tiles

| Field | Value |
|-------|-------|
| ID | e15s02 |
| Epic | e15 City Map on the Weather Page (M2, core) |
| Status | planned |
| BCPs | 6 |
| Risk | P0 |
| Type | feat |
| Delta | ADDED |

## 1. Summary
Add a `StationMap` component that renders the selected city on an interactive Leaflet map. Leaflet 1.9.4
is lazily loaded from unpkg (SRI-pinned) on first render, tiles come from OpenStreetMap, a default marker
opens a text-only popup, and a missing-coordinate case shows an explicit unavailable state. Also extend the
WebUI client mirror `WeatherReportModel` with nullable coordinates (ADR-0018 D2/D4/D6/D7).

## 2. Context
The page renders server-side (`InteractiveServer`) and the WebUI uses colocated ES modules for JS interop
(`RawJsonInspectorModal.razor.js` is the precedent: `import` the module, invoke exports, dispose the
module). `WeatherReportModel` is the client mirror of `WeatherReportDto` and is deserialised from the
same-origin API. Leaflet's own docs note popup content is HTML, so the popup is built from DOM text nodes.

## 3. User story
As a user, after seeing the current conditions I want to see the city located on a map, so I can orient the
weather at a glance.

## 4. Business value
The visible feature requested: a real, provider-accurate map with no API key and minimal runtime cost
(Leaflet loads only on this page).

## 5. In scope
- `WeatherReportModel` gains `decimal? Latitude`, `decimal? Longitude` and a `HasCoordinates` helper.
- `Components/Explorer/StationMap.razor`: section shell, coordinate caption, fixed-height map container,
  unavailable state.
- `Components/Explorer/StationMap.razor.js`: lazy CDN loader (SRI + crossorigin), `L.map` + OSM tile
  layer + marker + text popup, `setView` on coordinate change, `dispose` on teardown.
- Rebuild the committed Tailwind CSS for the new fixed-height classes.
- WebUI unit tests for the client mirror.

## 6. Out of scope
Parsing/serving coordinates (e15s01); CSP allow-listing (e15s03); placement on the page and the hero-card
switch (e15s04); forecast coordinates; custom icons; multiple markers.

## 7. Domain concepts
`WeatherReportModel` (client mirror), `StationMap` (component).

## 8. Layer touchpoints
| Assembly | Change |
|----------|--------|
| Weather.WebUI | `Services/WeatherReportModel.cs`; `Components/Explorer/StationMap.razor`; `Components/Explorer/StationMap.razor.js`; `wwwroot/app.css` (regenerated) |

## 9. API contract (consumed)
`GET /api/weather/{cityName}` (now carries `latitude`/`longitude`, e15s01).

## 10. Dependencies
e15s01 (coordinates in the payload); e15s03 must land before the map is visible in a browser (CSP).

## 11. Risks
| Risk | Mitigation |
|------|-----------|
| Popup HTML injection (Leaflet renders popup HTML) | Build the popup with `document.createElement` + `textContent`; never interpolate into `bindPopup` markup |
| Map instance leak on re-render / circuit teardown | Key on coordinates (`_renderedKey`), reuse the instance via `setView`, `map.remove()` in `dispose`, swallow `JSDisconnectedException` |
| Zero-height container (Leaflet needs a height) | Explicit fixed-height classes; Tailwind CSS rebuilt and committed |
| CDN script blocked or offline | Unavailable state is separate from tile failure; the page still renders; tiles degrade to empty |
| `0,0` mistaken for a real location | Nullable mirror + `HasCoordinates`; never draw at `0,0` |

## 12. Requirements (delta)
- **ADDED** — City map.
  **Requirement:** when the current report carries usable coordinates, render a `StationMap` with
  OpenStreetMap tiles and a marker at those coordinates, lazily loading Leaflet 1.9.4 from unpkg; otherwise
  render "Map not available — coordinates only.".

## 13. Implementation steps
1. Add `decimal? Latitude`/`decimal? Longitude` + `HasCoordinates` to `WeatherReportModel`; update the WebUI JSON fixtures and assert parsing when present and `HasCoordinates == false` when absent → verify: `dotnet test src/Tests/Weather.WebUI.UnitTests --filter "Category=WeatherApiClient|Category=WeatherExplorerModel"`
2. Add `StationMap.razor` (section, caption, fixed-height `@ref` container, unavailable state, `OnAfterRenderAsync` module import + `render`, `IAsyncDisposable` `dispose`) and `StationMap.razor.js` (lazy CDN loader, OSM tiles, marker, text popup, `setView`, `dispose`) → verify: `dotnet build Weather.sln --warnaserror && dotnet format Weather.sln --verify-no-changes`
3. Rebuild and commit the Tailwind stylesheet for the new arbitrary-height classes → verify: `bash scripts/check-tailwind-css.sh`
4. Story gate: full preflight green → verify: `dotnet build Weather.sln --warnaserror && dotnet format Weather.sln --verify-no-changes && dotnet test Weather.sln`

## 14. Test plan
- Unit (`WeatherApiClient` / `WeatherExplorerModel`): a report JSON with `latitude`/`longitude` populates the model and `HasCoordinates` is true; a report JSON without them yields `null` coordinates and `HasCoordinates == false` (never `0,0`).
- Build/format: the component and module compile and format clean.
- Manual (browser): select a city; confirm the marker sits at the city, the popup shows `City, Country — °C / °F` on click, changing city recentres the map, and navigating away removes the map without a JS exception.
- Manual (offline / blocked CDN): confirm the section shows the unavailable state or an empty tile canvas without breaking the page.

## 15. Acceptance criteria (Gherkin)
```gherkin
Scenario: Draw the map
  Given the report for the selected city has latitude and longitude
  When the station map renders
  Then OpenStreetMap tiles are shown centred on the city
  And a marker is placed at the coordinates

Scenario: Popup is text only
  Given a city named with HTML-significant characters
  When the marker popup opens
  Then the name is shown literally, not interpreted as HTML

Scenario: Missing coordinates
  Given the report has no latitude or longitude
  Then the section shows "Map not available — coordinates only."
  And no map is initialised

Scenario: Change city
  Given a map is already drawn
  When a different city is selected
  Then the existing map recentres without creating a second map instance
```

## 16. Definition of done
Map renders from report coordinates; Leaflet loads lazily and only on this page; text-only popup; graceful
unavailable state; Preflight green.

## 17. Verification script
1. `dotnet run --project src/Weather.WebUI`.
2. Select a country and city; confirm the map, marker, and popup.
3. Select another city; confirm the map recentres.
4. Simulate a report without coordinates; confirm the unavailable message and no map.

## 18. Risks / notes
The dotnet test suite has no browser/JS runtime, so tile/marker/popup behaviour is verified manually and is
not part of the offline gates; the nullable-mirror logic is unit-tested.

## 19. Sizing / BCP rationale
6 BCPs — client mirror + component + JS module + loader/dispose logic + Tailwind rebuild + tests.

## 20. References
- `specs/adr/0018-city-map-leaflet.md` (D2, D4, D6, D7)
- `src/Weather.WebUI/Components/Favorites/RawJsonInspectorModal.razor(.js)` (colocated-module precedent)
- Leaflet quick-start: `https://leafletjs.com/examples/quick-start/`
