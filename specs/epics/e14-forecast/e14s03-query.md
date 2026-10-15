# Story e14s03 — GetWeatherForecast query + IWeatherService + GET /api/weather/{cityName}/forecast

| Field | Value |
|-------|-------|
| ID | e14s03 |
| Epic | e14 Five-Day Forecast (M2, core) |
| Status | planned |
| BCPs | 4 |
| Risk | P0 |
| Type | feat |
| Delta | ADDED |

## 1. Summary
Expose `GET /api/weather/{cityName}/forecast?countryCode=` by sending a `GetWeatherForecastQuery` through
`IWeatherService`, mapping `WeatherForecast` to `WeatherForecastDto` (first five days), and mapping failures
to Problem Details.

## 2. Context
Mirrors e03s03 for the new forecast capability. The host stays thin: bind → `IWeatherService` → map result.
The current-weather endpoint is untouched.

## 3. User story
As a user, I want a city's five-day forecast, so the strip can render real daily cards.

## 4. Business value
Completes the read API the WebUI consumes; the second vertical slice of the Weather module.

## 5. In scope
- `GetWeatherForecastQuery` + validator (same rules as current weather) + handler.
- `WeatherForecastDto` / `ForecastDayDto` + `WeatherForecastDtoMapper` (take the first 5 days).
- `IWeatherService.GetForecastAsync` + `WeatherService` implementation.
- `WeatherController` action with `[ProducesResponseType]` for 200/404/422/500/502.
- Integration + OpenAPI contract tests (offline).

## 6. Out of scope
Caching; hourly detail; provider changes (e14s02); UI (e14s04).

## 7. Domain concepts
`WeatherForecast` → `WeatherForecastDto`.

## 8. Layer touchpoints
| Assembly | Change |
|----------|--------|
| `Weather.Modules.Weather.Application` | `WeatherReports/GetWeatherForecast/`, `Contracts/IWeatherService` |
| `Weather.Modules.Weather.Infrastructure` | `WeatherService` (+ method) |
| `Weather.WebUI` (host) | `Api/Modules/Weather/WeatherController` (+ action) |

## 9. API contract
`GET /api/weather/London/forecast?countryCode=GB` → `200`:
```json
{
  "location": { "city": "London", "country": "GB" },
  "days": [
    { "date": "2026-10-15", "high": { "celsius": 14.4, "fahrenheit": 58.0 },
      "low": { "celsius": 8.9, "fahrenheit": 48.0 },
      "precipitationProbabilityPercent": 75, "skyCondition": "Rain" }
  ]
}
```
`404` unknown city; `422` invalid input; `502` provider unavailable; `500` unhandled.

## 10. Dependencies
e14s01, e14s02.

## 11. Risks
| Risk | Mitigation |
|------|-----------|
| `DateOnly` JSON shape surprises the client | Assert the ISO date in the integration test; the WebUI model uses `DateOnly` |
| More than five days returned | Mapper takes the first five; asserted |
| Provider errors surface as 500 | Map `WeatherProviderException` → 502 (existing handler) |

## 12. Implementation steps
1. Add query + validator + handler → verify: `dotnet test src/Modules/Weather/Tests/Weather.Modules.Weather.Tests --filter Category=GetWeatherForecast`
2. Add `WeatherForecastDto` + `ForecastDayDto` + mapper → verify: `dotnet test src/Modules/Weather/Tests/Weather.Modules.Weather.Tests --filter Category=WeatherForecastDto`
3. Add `IWeatherService.GetForecastAsync` + `WeatherService` → verify: `dotnet build src/Modules/Weather/Weather.Modules.Weather.Infrastructure/Weather.Modules.Weather.Infrastructure.csproj --warnaserror`
4. Add the controller action + `[ProducesResponseType]`; extend the stub provider + integration/OpenAPI tests → verify: `dotnet test src/Tests/Weather.IntegrationTests --filter Category=WeatherForecast`
5. Story gate: full preflight + layout check → verify: `dotnet build Weather.sln --warnaserror && dotnet format Weather.sln --verify-no-changes && dotnet test Weather.sln && bash scripts/check-layout.sh`

## 13. Test plan
- Unit: handler returns a DTO from a stubbed forecast; propagates `WeatherProviderException`;
  mapper truncates to five days; validator rejects blank/oversized city and bad country code.
- Integration (offline): happy path returns `location` + `days[5]` with every field; 404; 422; 502.
- OpenAPI: the new path declares exactly `200,404,422,500,502`.

## 14. Acceptance criteria (Gherkin)
```gherkin
Scenario: Forecast payload completeness
  Given the stub provider returns a five-day forecast for "London"
  When I GET /api/weather/London/forecast?countryCode=GB
  Then the response is 200
  And days has 5 entries each with date, high.celsius, low.celsius, precipitationProbabilityPercent, skyCondition

Scenario: Unknown city
  When I GET /api/weather/Nowhere/forecast
  Then the response is 404 with ProblemDetails

Scenario: Provider unavailable
  When I GET /api/weather/ProviderDown/forecast
  Then the response is 502 with ProblemDetails
```

## 15. Definition of done
Tasks green; payload + errors + contract covered; the action contains no logic beyond calling `IWeatherService`.

## 16. Verification script
1. `dotnet run --project src/Weather.WebUI` (key set) → `curl -s localhost:5000/api/weather/London/forecast | jq`.
2. `dotnet test src/Tests/Weather.IntegrationTests --filter Category=WeatherForecast`

## 17. Out of scope
Caching; localization; write side.

## 18. Risks / notes
The route must not collide with `GET /api/weather/{cityName}` — the segment `forecast` and template
`{cityName}/forecast` are unambiguous.

## 19. Sizing / BCP rationale
4 BCPs — query + DTO/mapper + contract + thin endpoint + tests.

## 20. References
- `specs/adr/0017-five-day-forecast.md`
- `specs/epics/e03-weather/e03s03-query.md`
- `specs/adr/0011-rest-api-response-contract.md`
