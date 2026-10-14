# Story e03s03 — GetCurrentWeather query + IWeatherModule + GET /api/weather/{cityName}

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
city name. The API host stays thin — it only calls `IWeatherModule`.

## 3. User story
As a user, I want current weather for a city, so I can see its metrics.

## 4. Business value
Core feature; the end-to-end vertical slice of the Weather module.

## 5. In scope
- `GetCurrentWeatherQuery` + `GetCurrentWeatherQueryValidator` + `GetCurrentWeatherQueryHandler`.
- Module-local `ValidationBehavior<TRequest,TResponse>`.
- `WeatherReportDto` (+ `LocationDto`, `TemperatureDto`, `WindDto`) and `WeatherReportDtoMapper`.
- `IWeatherModule` contract + `WeatherModule` implementation (`ISender`).
- `Weather.Api/Modules/Weather/WeatherEndpoints.cs` + `Program.cs` wiring.
- Error mapping: `null` (city not found) → 404, `WeatherProviderException` → 502, `ValidationException` → 400.
- Endpoint tests in `src/Tests/Weather.IntegrationTests` (offline).

## 6. Out of scope
Caching; forecast; localization; favorite/notes modules.

## 7. Domain concepts
`WeatherReport` → `WeatherReportDto`.

## 8. Layer touchpoints
| Assembly | Change |
|----------|--------|
| `Weather.Modules.Weather.Application` | `WeatherReports/GetCurrentWeather/`, `Contracts/IWeatherModule`, `Configuration/ValidationBehavior` |
| `Weather.Modules.Weather.Infrastructure` | `WeatherModule` (implements `IWeatherModule`) |
| `Weather.Api` | `Modules/Weather/WeatherEndpoints`, `Program` wiring + exception handler |

## 9. API contract
`GET /api/weather/London?countryCode=GB` → `200`:
```json
{
  "location": { "city": "London", "country": "GB" },
  "observedAtUtc": "2025-10-13T12:00:00Z",
  "temperature": { "celsius": 12.5, "fahrenheit": 54.5 },
  "dewPoint": { "celsius": 9.1, "fahrenheit": 48.4 },
  "relativeHumidityPercent": 79,
  "wind": { "speedMetersPerSecond": 4.1, "directionDegrees": 220, "cardinal": "SW" },
  // directionDegrees is null and cardinal is "Variable" when the provider omits wind.deg
  "visibilityMeters": 10000,
  "pressureHectopascals": 1013,
  "skyCondition": "Cloudy"
}
```
`404` unknown city; `502` provider unavailable; `400` invalid input.

## 10. Dependencies
e03s01, e03s02.

## 11. Risks
| Risk | Mitigation |
|------|-----------|
| Provider errors surface as 500 | Map `WeatherProviderException` → 502 |
| Missing metrics in payload | DTO non-nullable; absence throws + tested |
| Logic creeping into the endpoint | Endpoint only calls `IWeatherModule` and maps results |

## 12. Implementation steps
1. Add `GetCurrentWeatherQuery` + validator + handler → verify: `dotnet test src/Modules/Weather/Tests/Weather.Modules.Weather.Tests --filter Category=GetCurrentWeather`
2. Add module-local `ValidationBehavior` → verify: `dotnet test src/Modules/Weather/Tests/Weather.Modules.Weather.Tests --filter Category=ValidationBehavior`
3. Add `WeatherReportDto` + `WeatherReportDtoMapper` → verify: `dotnet test src/Modules/Weather/Tests/Weather.Modules.Weather.Tests --filter Category=WeatherReportDto`
4. Add `IWeatherModule` + `WeatherModule` impl → verify: `dotnet build src/Modules/Weather/Weather.Modules.Weather.Infrastructure/Weather.Modules.Weather.Infrastructure.csproj --warnaserror`
5. Add endpoint + `Program.cs` wiring + exception handler → verify: `dotnet test src/Tests/Weather.IntegrationTests --filter Category=WeatherEndpoint`
6. Assert payload completeness + error mapping (404/502/400) → verify: `dotnet test src/Tests/Weather.IntegrationTests --filter Category=WeatherErrors`

## 13. Test plan
- Unit: handler returns a DTO for a stubbed report; propagates `WeatherProviderException`; validator rejects blank/oversized city and bad country code.
- Integration (offline): happy path returns every required field; unknown city → 404; stub failure → 502; invalid input → 400.

## 14. Acceptance criteria (Gherkin)
```gherkin
Scenario: Weather payload completeness
  Given the stub provider returns a report for "London"
  When I GET /api/weather/London
  Then the response is 200
  And the body has location, observedAtUtc, temperature.celsius, temperature.fahrenheit,
      dewPoint.celsius, relativeHumidityPercent, wind.speedMetersPerSecond, wind.directionDegrees,
      visibilityMeters, pressureHectopascals, skyCondition

Scenario: Unknown city
  Given the provider reports not found
  When I GET /api/weather/Nowhere
  Then the response is 404 with ProblemDetails

Scenario: Provider unavailable
  Given the provider is unavailable
  When I GET /api/weather/London
  Then the response is 502 with ProblemDetails

Scenario: Invalid input
  When I GET /api/weather/ with an oversized city name
  Then the response is 400 with ProblemDetails
```

## 15. Definition of done
Tasks green; payload + errors covered; the endpoint contains no logic beyond calling `IWeatherModule`.

## 16. Verification script
1. `dotnet run --project src/Weather.Api` (key set) → `curl -s localhost:5000/api/weather/London | jq`.
2. `dotnet test src/Tests/Weather.IntegrationTests --filter Category=Weather`.

## 17. Out of scope
Caching; localization; write side.

## 18. Risks / notes
Live demo needs an OWM API key; tests never do.

## 19. Sizing / BCP rationale
4 BCPs — query + contract + thin endpoint + error mapping.

## 20. References
- `specs/adr/0007-weather-module-read-only-self-contained.md`
- `specs/tech-architecture/DESIGN_PLAN_LATEST.md`
- `specs/tech-architecture/TEST_PLAN_LATEST.md`
