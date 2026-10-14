# Story e03s03 — GetCurrentWeather query + GET /api/weather/{cityName}

| Field | Value |
|-------|-------|
| ID | e03s03 |
| Epic | e03 Weather Module (M2, core) |
| Status | todo |
| BCPs | 4 |
| Risk | P0 |
| Type | feat |

## 1. Summary
Expose `GET /api/weather/{cityName}?countryCode=` by sending a `GetCurrentWeatherQuery` into the Weather module
through `IWeatherModule`, mapping the domain report to `WeatherReportDto`, and mapping failures to ProblemDetails.

## 2. Context
The primary user-facing read and the reason the app exists. Weather stays independent: the provider geocodes the
city name.

## 3. User story
As a user, I want current weather for a city, so I can see its metrics.

## 4. Business value
Core feature.

## 5. In scope
- `WeatherReports/GetCurrentWeather` query + validator + handler (`IWeatherService`).
- Domain → `WeatherReportDto` mapping (location, UTC, temp C/F, dew point C/F, humidity, wind, visibility, pressure, sky).
- `IWeatherModule.GetCurrentWeatherAsync`.
- Host endpoint in the Weather area; `404`/`502`/`400` mappings.

## 6. Out of scope
Caching; forecast.

## 7. Domain concepts
`WeatherReport` → `WeatherReportDto`.

## 8. Layer touchpoints
| Assembly | Change |
|----------|--------|
| Weather.Application | `WeatherReports/GetCurrentWeather`, `Contracts/IWeatherModule` |
| Weather.Infrastructure | `WeatherModule` handles the query |
| Weather.Api | Weather endpoint area |

## 9. API contract
`GET /api/weather/London?countryCode=GB` → `200` full payload; `404` unknown; `502` provider down; `400` invalid.

## 10. Dependencies
e03s01, e03s02.

## 11. Risks
| Risk | Mitigation |
|------|-----------|
| Provider errors as 500 | Map `WeatherProviderException` → 502 |
| Missing metrics | DTO non-nullable; absence throws + tested |

## 12. Implementation steps
1. Add `WeatherReportDto` + mapping → verify: `dotnet test src/Modules/Weather/Tests --filter Category=WeatherDto`
2. Add `GetCurrentWeather` query + validator + handler → verify: `dotnet test src/Modules/Weather/Tests --filter Category=GetWeather`
3. Implement `IWeatherModule.GetCurrentWeatherAsync` → verify: `dotnet test src/Modules/Weather/Tests --filter Category=WeatherModule`
4. Add the host endpoint → verify: `dotnet test src/Tests/IntegrationTests --filter Category=Weather`
5. Map 404/502/400 → verify: `dotnet test src/Tests/IntegrationTests --filter Category=WeatherErrors`
6. Assert payload completeness → verify: `dotnet test src/Tests/IntegrationTests --filter Category=WeatherPayload`

## 13. Test plan
SC-e03s03-P0-01/P1-01. Handler with stub service; endpoint happy path; 404; 502; payload completeness.

## 14. Acceptance criteria (Gherkin)
```gherkin
Scenario: Weather payload completeness
  Given the stub provider returns a report for "London"
  When I GET /api/weather/London
  Then 200 and the body has location, observedAtUtc, temperature.celsius, temperature.fahrenheit,
       dewPoint.celsius, relativeHumidityPercent, wind.speedMetersPerSecond, wind.directionDegrees,
       visibilityMeters, pressureHectopascals, skyCondition

Scenario: Unknown city
  Given the provider reports not found
  Then 404 with ProblemDetails

Scenario: Provider unavailable
  Then 502 with ProblemDetails
```

## 15. Definition of done
Tasks green; payload + errors covered.

## 16. Verification script
1. `dotnet run --project src/Weather.Api` (key set) → `curl localhost:5000/api/weather/London`.
2. `dotnet test src/Tests/IntegrationTests --filter Category=Weather`.

## 17. Out of scope
Caching; localization.

## 18. Risks / notes
Live demo needs a key; tests never do.

## 19. Sizing / BCP rationale
4 BCPs — feature read + error mapping.

## 20. References
- `specs/tech-architecture/DESIGN_PLAN_LATEST.md`
- `specs/tech-architecture/TEST_PLAN_LATEST.md`
