# Story e14s04 — WebUI 5-day forecast strip (ForecastStrip + ForecastDayCard)

| Field | Value |
|-------|-------|
| ID | e14s04 |
| Epic | e14 Five-Day Forecast (M2, core) |
| Status | planned |
| BCPs | 5 |
| Risk | P1 |
| Type | feat |
| Delta | MODIFIED |

## 1. Summary
Replace the empty `ForecastStrip` fallback with the design's five daily cards: the page loads the forecast
alongside current conditions, `ForecastStrip` renders one `ForecastDayCard` per day (weekday label, icon,
high/low, precipitation probability), and a forecast failure degrades gracefully.

## 2. Context
`ForecastStrip` currently renders a "Forecast not available — current conditions only" shell (e06s03 D3).
`WeatherApiClient` and `WeatherExplorerModel` already drive the current-weather cascade. The design
(`specs/designs/weather_explorer_asp.net_core_client/code.html`, "5-Day Predictive Outlook Strip") shows five
cards with a weekday label, condition icon, `high° / low°`, and an umbrella + percentage.

## 3. User story
As a user, when I pick a city I want to see its five-day outlook in the strip beneath current conditions.

## 4. Business value
Delivers the visible feature the user asked for, using only real API data.

## 5. In scope
- `WeatherForecastModel` / `ForecastDayModel` client mirrors.
- `WeatherApiClient.GetForecastAsync(cityName, countryCode)`.
- `WeatherExplorerModel.Forecast`; `LoadReportAsync` loads current + forecast; forecast failure does **not**
  blank current conditions (D5).
- `ForecastDayCard` component; `ForecastStrip` renders the cards (or the existing unavailable state).
- Sky-condition → Material Symbols icon mapping (small static helper).
- WebUI unit + page integration tests.

## 6. Out of scope
Hourly/3-hour detail; charts; provider/API changes (e14s01-s03); styling system changes.

## 7. Domain concepts
`WeatherForecastModel`, `ForecastDayModel`, existing `WeatherReportModel`.

## 8. Layer touchpoints
| Assembly | Change |
|----------|--------|
| Weather.WebUI | `Services/*`, `Components/Explorer/ForecastStrip.razor`, `Components/Explorer/ForecastDayCard.razor` |

## 9. API contract (consumed)
`GET /api/weather/{cityName}/forecast?countryCode=`.

## 10. Dependencies
e14s03.

## 11. Risks
| Risk | Mitigation |
|------|-----------|
| Forecast failure hides current conditions | Load independently; keep `Report`; strip shows unavailable |
| Fabricated/again-empty strip | Cards render only from `model.Forecast.Days`; no placeholder numbers |
| `'unsafe-inline'` creep | Keep classes only, no inline `style` |
| Icon mismatch | Central mapping helper, covered by a unit test |

## 12. Requirements (delta)
- **MODIFIED** — Forecast strip availability.
  **Before:** `ForecastStrip` always showed "Forecast not available — current conditions only".
  **After:** `ForecastStrip` renders five `ForecastDayCard`s when `Forecast` is present; the unavailable
  message is shown only when the forecast could not be loaded.

## 13. Implementation steps
1. Add `WeatherForecastModel` + `ForecastDayModel` + `WeatherApiClient.GetForecastAsync` → verify: `dotnet test src/Tests/Weather.WebUI.UnitTests --filter Category=WeatherApiClient`
2. Add `WeatherExplorerModel.Forecast` and load it alongside the report (independent failure) → verify: `dotnet test src/Tests/Weather.WebUI.UnitTests --filter Category=WeatherExplorerModel`
3. Add the sky-condition → icon helper with a unit test → verify: `dotnet test src/Tests/Weather.WebUI.UnitTests --filter Category=SkyConditionIcon`
4. Add `ForecastDayCard` + rewrite `ForecastStrip`; wire the page parameter → verify: `dotnet test src/Tests/Weather.IntegrationTests --filter Category=WeatherExplorerPage`
5. Story gate: full preflight + layout check → verify: `dotnet build Weather.sln --warnaserror && dotnet format Weather.sln --verify-no-changes && dotnet test Weather.sln && bash scripts/check-layout.sh`

## 14. Test plan
- Unit: client builds the right relative URI; the model loads forecast + report; forecast failure keeps the
  report and leaves `Forecast` null; icon helper maps every `SkyCondition` value.
- Integration: `GET /` still renders and the forecast headings are present; the page has no exception.
- Manual: select a city; confirm five cards with weekday, icon, high/low, and rain chance; stop the provider
  (no key) and confirm current conditions remain while the strip shows unavailable.

## 15. Acceptance criteria (Gherkin)
```gherkin
Scenario: Render the forecast
  Given a city is selected and the API returns a five-day forecast
  Then the strip shows five cards
  And each card shows a weekday label, a high/low, and a precipitation probability

Scenario: Forecast failure does not blank current conditions
  Given current conditions load but the forecast call fails
  Then the current conditions still render
  And the strip shows "Forecast not available"

Scenario: No fabricated data
  Given no forecast has loaded
  Then the strip shows the unavailable state and no temperature numbers
```

## 16. Definition of done
Strip matches the design's grid and card structure; only real data; Preflight green.

## 17. Verification script
1. `dotnet run --project src/Weather.WebUI`.
2. Select a country then a city; confirm five forecast cards render.
3. Remove the key; confirm current conditions remain and the strip shows the unavailable state.

## 18. Risks / notes
Weekday labels come from the API date (`ddd`), not the client clock, so tests are deterministic.

## 19. Sizing / BCP rationale
5 BCPs — models + client + model orchestration + two components + icon helper.

## 20. References
- `specs/adr/0017-five-day-forecast.md`
- `specs/tech-architecture/UI_DESIGN_PLAN_LATEST.md` §5/§6
- `specs/designs/weather_explorer_asp.net_core_client/code.html` (5-Day Predictive Outlook Strip)
