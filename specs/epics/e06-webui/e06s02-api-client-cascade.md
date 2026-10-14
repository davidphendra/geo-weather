# Story e06s02 — WeatherApiClient + country→city cascade

| Field | Value |
|-------|-------|
| ID | e06s02 |
| Epic | e06 WebUI Front-End |
| Status | todo |
| BCPs | 5 |
| Risk | P1 |
| Type | feat |

## 1. Summary
Add a typed `WeatherApiClient` over the same-origin REST API, client-side DTO mirrors, DI wiring (origin from
`NavigationManager.BaseUri`, no `Api:BaseAddress`), and a unit-testable cascade/loading state model.

## 2. Context
The UI consumes the API HTTP contract only (ADR-0012). Components run server-side (`InteractiveServer`), so the
`HttpClient` needs an absolute origin: it is derived at runtime from `NavigationManager.BaseUri` (D7), not from
configuration. The cascade (country → cities → weather) is modelled so it is testable without a browser.

## 3. User story
As a user, I want to pick a country then a city, so the app can request that city's weather.

## 4. Business value
First end-to-end data path; every page depends on this client.

## 5. In scope
- `Services/WeatherApiClient` (`GetCountriesAsync`, `GetCitiesAsync`, `GetWeatherAsync`, `AddFavoriteAsync`,
  `GetFavoritesAsync`, `AddNoteAsync`) with absolute URIs built from `NavigationManager.BaseUri`.
- Client DTO mirrors (`CountryModel`, `CityModel`, `WeatherReportModel`, `LocationModel`, `TemperatureModel`,
  `WindModel`, `FavoriteCityModel`, `AddFavoriteModel`, `AddNoteModel`, `ApiProblem`).
- ProblemDetails-aware failures (`WeatherApiException` with `status` + `code`).
- `AddHttpClient<WeatherApiClient>()` — **no** `Api:BaseAddress`.
- Unit-testable query/cascade state (`WeatherExplorerModel`) with loading and error states.
- Client unit tests with a stubbed `HttpMessageHandler`.

## 6. Out of scope
Page layout/rendering (e06s03/e06s04); CSP; visual parity.

## 7. Domain concepts
`CountryModel`, `CityModel`, `WeatherReportModel` — client-side mirrors of the API contracts (never module types).

## 8. Layer touchpoints
| Assembly | Change |
|----------|--------|
| Weather.WebUI | `Services/WeatherApiClient.cs`, `Services/*Model.cs`, `Services/WeatherExplorerModel.cs`, DI in `Program.cs` |

## 9. API contract (consumed)
`GET /api/countries`; `GET /api/countries/{countryCode}/cities`; `GET /api/weather/{cityName}?countryCode=`; `POST /api/cities/favorites`; `GET /api/cities/favorites`; `POST /api/weather/notes`.

## 10. Dependencies
e06s01, e02s01, e02s02, e03s03, e04s01.

## 11. Risks
| Risk | Mitigation |
|------|-----------|
| Relative URI under server-side render | Build absolute URIs from `NavigationManager.BaseUri` (D7) |
| Base address via config | Explicitly none; test asserts no `Api:BaseAddress` usage |
| Socket exhaustion | `AddHttpClient` factory-managed handler, not `new HttpClient()` per scope |

## 12. Implementation steps
1. Add the client DTO mirrors → verify: `dotnet build src/Weather.WebUI/Weather.WebUI.csproj --warnaserror`
2. Add `WeatherApiClient` (absolute URIs from `NavigationManager.BaseUri`) and ProblemDetails failure mapping → verify: `dotnet test src/Tests/Weather.WebUI.UnitTests --filter Category=WeatherApiClient`
3. Register `AddHttpClient<WeatherApiClient>()` (no `Api:BaseAddress`) → verify: `! grep -rn "Api:BaseAddress" src/Weather.WebUI`
4. Add `WeatherExplorerModel` cascade/loading/error state → verify: `dotnet test src/Tests/Weather.WebUI.UnitTests --filter Category=WeatherExplorerModel`
5. Add client tests with a stubbed `HttpMessageHandler` (success + 404 + 502) → verify: `dotnet test src/Tests/Weather.WebUI.UnitTests --filter Category=WeatherApiClient`

## 13. Test plan
- Client parses countries/cities/weather from stub JSON; maps a 404/502 ProblemDetails to `WeatherApiException`.
- Cascade model: selecting a country loads its cities; selecting a city triggers a weather fetch; error state surfaces.

## 14. Acceptance criteria (Gherkin)
```gherkin
Scenario: Cascade works
  Given the API returns countries and cities
  When I select "United Kingdom"
  Then the city list loads

Scenario: Upstream failure is surfaced
  Given the weather API returns 502 with a ProblemDetails body
  When I select a city
  Then the model exposes an error state carrying the problem code
```

## 15. Definition of done
Client + model built and unit-tested; no configurable base address; Preflight green.

## 16. Verification script
1. `dotnet test src/Tests/Weather.WebUI.UnitTests --filter Category=WeatherApiClient`.

## 17. Out of scope
UI markup; caching; retries beyond the existing resilience posture.

## 18. Risks / notes
Distinguish client validation from server ProblemDetails; never leak the response body to users as HTML.

## 19. Sizing / BCP rationale
5 BCPs — typed client + DTOs + cascade state + tests.

## 20. References
- `specs/tech-architecture/UI_DESIGN_PLAN_LATEST.md` §7a, §12 (D7/Q1)
- `specs/adr/0012-merge-api-into-webui.md`
