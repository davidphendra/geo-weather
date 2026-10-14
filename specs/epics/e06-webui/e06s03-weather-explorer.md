# Story e06s03 — Weather Explorer page

| Field | Value |
|-------|-------|
| ID | e06s03 |
| Epic | e06 WebUI Front-End |
| Status | todo |
| BCPs | 7 |
| Risk | P1 |
| Type | feat |

## 1. Summary
Build the `/` **Weather Explorer** page to match `specs/designs/weather_explorer_asp.net_core_client/code.html`:
query controller, hero station card, diagnostic grid, forecast shell, command dispatch form, response well, and
the architecture drawer.

## 2. Context
The prototype page is a telemetry console. Real data comes from `WeatherApiClient`; where the API has no field,
the page keeps the design's layout but shows a substituted real metric or an explicit "not available" state (D3),
never fabricated data.

## 3. User story
As a user, I want to select a city and see its current weather in the telemetry console layout, and dispatch a
favorite/note command with visible feedback.

## 4. Business value
The primary user path and the page both prototypes center on.

## 5. In scope
- `WeatherExplorer.razor` (`@page "/"`, `@rendermode InteractiveServer`) at `max-w-[1600px]`, 7/5 grid.
- `QueryControllerPanel` (country/city selects, Fetch), `MediatRExpressionBar` (static label).
- `StationHeroCard` + `DiagnosticMetricGrid`/`DiagnosticCell`: temp C/F, condition, humidity + dew point,
  wind + cardinal, pressure + observed-at UTC, **visibility** (replaces UV/solar), no 24h sparkline series.
- `ForecastStrip` shell with "Forecast not available — current conditions only".
- `CommandDispatchForm` with **Favorite ⇄ Note** intent toggle (default Favorite) posting to the matching endpoint.
- `ChangeTrackerResponseWell` rendering the real HTTP status + response JSON.
- `ArchitectureDeepDive` (cURL / C# tabs, real route paths, static content).
- Loading / error / not-found states.

## 6. Out of scope
Favorites page (e06s04); forecast/UV data (no backend source; out of scope per `SCOPE_LATEST.yaml`); CSP.

## 7. Domain concepts
`WeatherReportModel`, `CityModel`, `CountryModel`, `FavoriteCityModel`, `WeatherNoteModel`.

## 8. Layer touchpoints
| Assembly | Change |
|----------|--------|
| Weather.WebUI | `Components/Pages/WeatherExplorer.razor`, `Components/Explorer/*` |

## 9. API contract (consumed)
`GET /api/countries`; `GET /api/countries/{countryCode}/cities`; `GET /api/weather/{cityName}?countryCode=`; `POST /api/cities/favorites`; `POST /api/weather/notes`.

## 10. Dependencies
e06s01, e06s02.

## 11. Risks
| Risk | Mitigation |
|------|-----------|
| Design shows metrics the API lacks | D3 substitution table; explicit unavailable states |
| Double submit | Disable the submit button while posting |
| Prototype route labels wrong (`/api/v1/…`) | Render real routes |
| `'unsafe-inline'` creep | No inline `style`; `.icon-fill` class only |

## 12. Implementation steps
1. Build `QueryControllerPanel` + `MediatRExpressionBar` → verify: `dotnet build src/Weather.WebUI/Weather.WebUI.csproj --warnaserror`
2. Build `StationHeroCard` + `DiagnosticMetricGrid`/`DiagnosticCell` with the D3 substitutions → verify: `dotnet build src/Weather.WebUI/Weather.WebUI.csproj --warnaserror`
3. Build `ForecastStrip` unavailable shell → verify: `dotnet build src/Weather.WebUI/Weather.WebUI.csproj --warnaserror`
4. Build `CommandDispatchForm` (Favorite/Note toggle) + `ChangeTrackerResponseWell` → verify: `dotnet test src/Tests/Weather.WebUI.UnitTests --filter Category=CommandDispatch`
5. Build `ArchitectureDeepDive` → verify: `dotnet build src/Weather.WebUI/Weather.WebUI.csproj --warnaserror`
6. Wire the page (loading/error/not-found states) → verify: `dotnet test src/Tests/Weather.IntegrationTests --filter Category=WeatherExplorerPage`

## 13. Test plan
- Unit: command form maps Favorite → `AddFavoriteAsync` and Note → `AddNoteAsync`; validation blocks empty input.
- Integration: `GET /` returns the shell markers; no exception.
- Manual: select country → city → metrics render; dispatch a favorite → `201` JSON in the response well.

## 14. Acceptance criteria (Gherkin)
```gherkin
Scenario: Render weather
  Given a city is selected
  When the API returns a report
  Then the page shows temperature C/F, condition, humidity + dew point, wind, pressure and visibility

Scenario: Save a favorite
  Given the intent is Favorite
  When I submit a valid alias
  Then the response well shows HTTP 201 and the created resource

Scenario: Save a note
  Given the intent is Note
  When I submit a valid note
  Then the response well shows HTTP 201 and the created resource

Scenario: Forecast is not fabricated
  Given the API has no forecast data
  Then the forecast strip shows "Forecast not available"
```

## 15. Definition of done
`/` matches the prototype layout/styles; real data only; command paths work; Preflight green.

## 16. Verification script
1. `dotnet run --project src/Weather.WebUI`.
2. Select a country then a city; confirm all diagnostics render.
3. Dispatch a Favorite, then a Note; confirm `201` payloads in the response well.

## 17. Out of scope
Editing/deleting notes; forecast data; real-time refresh (manual Fetch only).

## 18. Risks / notes
The MediatR/pipeline chips are static labels describing the real pipeline; they must not claim runtime facts
(e.g. fake latency numbers) unless measured.

## 19. Sizing / BCP rationale
7 BCPs — one dense page with a form and several components.

## 20. References
- `specs/tech-architecture/UI_DESIGN_PLAN_LATEST.md` §5, §6
- `specs/designs/weather_explorer_asp.net_core_client/code.html`
