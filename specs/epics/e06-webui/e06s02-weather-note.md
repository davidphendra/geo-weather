# Story e06s02 — Weather render + save-note form with feedback

| Field | Value |
|-------|-------|
| ID | e06s02 |
| Epic | e06 WebUI |
| Status | todo |
| BCPs | 5 |
| Risk | P1 |
| Type | feat |

## 1. Summary
Trigger `GET /api/weather/{city}` on city selection, render all metrics as cards, and add a save-note form
posting to `POST /api/weather/notes` with success/error feedback.

## 2. Context
Completes the user workflow and exercises the write path from the UI.

## 3. User story
As a user, I want to see weather details and save a note.

## 4. Business value
Full end-to-end experience.

## 5. In scope
- Weather fetch on city selection; loading/error states.
- Metric cards: location, UTC time, temp C/F, dew point C/F, humidity, wind, visibility, pressure, sky.
- Validated save-note form; success message shows the created id.
- `WeatherApiClient.GetWeatherAsync`, `.AddNoteAsync`.

## 6. Out of scope
Favorite UI (unless e04 is demoed); theming.

## 7. Domain concepts
`WeatherReportDto`, `AddNoteRequest`.

## 8. Layer touchpoints
| Assembly | Change |
|----------|--------|
| Weather.WebUI | `Weather.razor` render + form; `WeatherApiClient` methods |

## 9. API contract (consumed)
`GET /api/weather/{cityName}?countryCode=`; `POST /api/weather/notes`.

## 10. Dependencies
e06s01, e03s03, e05s01.

## 11. Risks
| Risk | Mitigation |
|------|-----------|
| Missing metrics render blank | DTO non-nullable; fallback text |
| Double submit | Disable button while posting |

## 12. Implementation steps
1. Add `GetWeatherAsync` and trigger on selection → verify: `dotnet build src/Weather.WebUI`
2. Render metric cards → verify: `dotnet build src/Weather.WebUI`
3. Add validated save-note form + `AddNoteAsync` → verify: `dotnet build src/Weather.WebUI`
4. Add success/error feedback → verify: `dotnet build src/Weather.WebUI`
5. Unit test client methods with a stub handler → verify: `dotnet test src/Tests/IntegrationTests --filter Category=ApiClient`

## 13. Test plan
- Client parses a weather payload; posts a note and reads the id.
- Manual: select city → metrics; submit note → confirmation.

## 14. Acceptance criteria (Gherkin)
```gherkin
Scenario: Render weather
  Given a city is selected
  When the API returns a report
  Then the page shows temperature C/F, dew point, humidity, wind, visibility, pressure, sky

Scenario: Save a note
  Given weather is displayed
  When I submit a valid note
  Then a success message shows the created id

Scenario: Reject empty note in UI
  Given the note field is empty
  Then a validation message is shown and no request is sent
```

## 15. Definition of done
All metrics render; note save shows feedback; client tests green.

## 16. Verification script
1. Run API + WebUI; select a city → metrics; enter a note → success with id.

## 17. Out of scope
Editing/deleting notes.

## 18. Risks / notes
Distinguish client validation from server errors.

## 19. Sizing / BCP rationale
5 BCPs — render + form + client + feedback.

## 20. References
- `specs/tech-architecture/DESIGN_PLAN_LATEST.md`
