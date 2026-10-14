# Story e06s01 — Country & city dropdowns wired to the API

| Field | Value |
|-------|-------|
| ID | e06s01 |
| Epic | e06 WebUI |
| Status | todo |
| BCPs | 5 |
| Risk | P1 |
| Type | feat |

## 1. Summary
Add a typed `WeatherApiClient` to the Blazor WebUI and build the Country → City cascade from
`GET /api/countries` and `GET /api/countries/{code}/cities`.

## 2. Context
The scaffolded `Weather.WebUI` is a default template; this story gives it a real API client and the first half of
the workflow.

## 3. User story
As a user, I want to pick a country then a city, so I can request weather.

## 4. Business value
First visible end-to-end path once Geography is live.

## 5. In scope
- `WeatherApiClient` (`GetCountriesAsync`, `GetCitiesAsync`) with configurable base address.
- `Weather.razor` country/city selects; loading and error states.
- Same-origin hosted API (no CORS needed — ADR-0012).

## 6. Out of scope
Weather rendering and saving (e06s02).

## 7. Domain concepts
`CountryDto`, `CityDto` (client-side mirrors).

## 8. Layer touchpoints
| Assembly | Change |
|----------|--------|
| Weather.WebUI | `Services/WeatherApiClient`, `Components/Pages/Weather.razor`, DI in Program.cs |
| Weather.WebUI (host) | no CORS policy — the API is served on the same origin (ADR-0012) |

## 9. API contract (consumed)
`GET /api/countries`; `GET /api/countries/{code}/cities`.

## 10. Dependencies
e02s01, e02s02, e01s01 (WebUI moved).

## 11. Risks
| Risk | Mitigation |
|------|-----------|
| Base URL mismatch | Configure `BaseAddress` from the app's own origin (same-origin host, ADR-0012) |
| Async void | `async Task OnInitializedAsync` + null guards |

## 12. Implementation steps
1. Add `WeatherApiClient` + DTOs + DI → verify: `dotnet build Weather.sln --warnaserror`
2. Enable CORS for the WebUI origin → verify: `dotnet test src/Tests/IntegrationTests --filter Category=Cors`
3. Build the country dropdown → verify: `dotnet build src/Weather.WebUI`
4. Build the city cascade + loading/error states → verify: `dotnet build src/Weather.WebUI`
5. Unit test the client with a stubbed handler → verify: `dotnet test src/Tests/IntegrationTests --filter Category=ApiClient`

## 13. Test plan
- Client parses countries/cities from a stub handler.
- Manual: select country → cities appear.

## 14. Acceptance criteria (Gherkin)
```gherkin
Scenario: Cascade works
  Given the API returns countries and cities
  When I select "United Kingdom"
  Then the city dropdown shows its cities
```

## 15. Definition of done
WebUI builds; cascade works against a running API.

## 16. Verification script
1. Run API + WebUI; select a country; confirm cities load.

## 17. Out of scope
Persistence of selection; search.

## 18. Risks / notes
The client consumes the API's HTTP contract even though the UI and the host share one process (ADR-0012).

## 19. Sizing / BCP rationale
5 BCPs — typed client + component wiring.

## 20. References
- `specs/tech-architecture/DESIGN_PLAN_LATEST.md`
