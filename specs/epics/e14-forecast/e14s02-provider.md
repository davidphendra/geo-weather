# Story e14s02 — IWeatherProvider.GetForecastAsync + OpenWeatherMap 5 day/3 hour adapter

| Field | Value |
|-------|-------|
| ID | e14s02 |
| Epic | e14 Five-Day Forecast (M2, core) |
| Status | planned |
| BCPs | 4 |
| Risk | P0 |
| Type | feat |
| Delta | ADDED |

## 1. Summary
Add `GetForecastAsync` to `IWeatherProvider` and implement it in `OpenWeatherMapService` against
`data/2.5/forecast?q={city},{country}&units=imperial`, mapping the canned 3-hour payload to a domain
`WeatherForecast` via `city.timezone`.

## 2. Context
The typed client, API-key `DelegatingHandler`, and `OpenWeather:Resilience` pipeline already exist (e03s02,
e03s04). This story only adds a second relative URI and a second mapper. Verified provider facts:
`list[].main.temp` is Fahrenheit with `units=imperial`; `list[].pop` is 0..1; `city.timezone` is seconds from
UTC; `list[].dt` is unix UTC.

## 3. User story
As a developer, I want the forecast fetched behind the existing provider port, so tests stay offline and the
key never leaks.

## 4. Business value
Enables the forecast vertical slice with zero new infrastructure or secrets.

## 5. In scope
- `GetForecastAsync(string cityName, string? countryCode, CancellationToken)` on `IWeatherProvider`.
- `OpenWeatherForecastResponse` provider DTO (+ nested blocks) and `OpenWeatherForecastMapper`.
- `OpenWeatherMapService.GetForecastAsync`: same resilience/key handler, 404 → `null`,
  non-success/timeout/empty/malformed → `WeatherProviderException`.
- Metrics `weather.forecast.requests` + `weather.forecast.duration` in `WeatherMeters` (+ catalog update).
- Adapter tests with a canned `HttpMessageHandler` (no network).

## 6. Out of scope
Query/DTO/endpoint (e14s03); caching; One Call.

## 7. Domain concepts
`WeatherForecast`, `ForecastInterval`, `Temperature.FromFahrenheit`, `SkyCondition.FromProvider`.

## 8. Layer touchpoints
| Assembly | Change |
|----------|--------|
| `Weather.Modules.Weather.Application` | `Abstractions/IWeatherProvider` (+ method) |
| `Weather.Modules.Weather.Infrastructure` | `OpenWeatherMap/OpenWeatherForecastResponse`, `OpenWeatherForecastMapper`, `OpenWeatherMapService` |
| `Weather.BuildingBlocks.Application` | `Telemetry/WeatherMeters` (+ 2 instruments) |

## 9. API contract
Internal only (upstream `GET /data/2.5/forecast`).

## 10. Dependencies
e14s01.

## 11. Risks (security: medium — outbound + secret)
| Risk | Mitigation |
|------|-----------|
| API key in logs/exceptions | Reuse the existing key handler + per-client `Warning` log level; never log the URL |
| Provider down | Existing resilience pipeline; wrap as `WeatherProviderException` → 502 |
| Missing/odd fields (`pop`, `temp`) | Typed DTO; malformed body → `WeatherProviderException` |
| Wrong units | Assert mph→m/s only where shown; temp F→C on the mapper boundary; canned-JSON tests |

## 12. Implementation steps
1. Add `OpenWeatherForecastResponse` + nested DTOs → verify: `dotnet build src/Modules/Weather/Weather.Modules.Weather.Infrastructure/Weather.Modules.Weather.Infrastructure.csproj --warnaserror`
2. Add `OpenWeatherForecastMapper` (imperial → `ForecastInterval[]` + `WeatherForecast.FromIntervals` with `city.timezone`) → verify: `dotnet test src/Modules/Weather/Tests/Weather.Modules.Weather.Tests --filter Category=OpenWeatherForecastMapping`
3. Add `IWeatherProvider.GetForecastAsync` + service implementation (404 → null; 5xx/timeout/empty → `WeatherProviderException`) → verify: `dotnet test src/Modules/Weather/Tests/Weather.Modules.Weather.Tests --filter Category=OpenWeatherForecastService`
4. Add `weather.forecast.requests` / `weather.forecast.duration` instruments + emission and update the catalog → verify: `dotnet test src/Modules/Weather/Tests/Weather.Modules.Weather.Tests --filter Category=ForecastObservability`
5. Story gate: full preflight + layout check → verify: `dotnet build Weather.sln --warnaserror && dotnet format Weather.sln --verify-no-changes && dotnet test Weather.sln && bash scripts/check-layout.sh`

## 13. Test plan
- Mapping: canned 40-entry payload → 5 days; temp F→C; `pop` 0.75 → 75; `city.timezone` shifts grouping.
- Service: 200 → `WeatherForecast`; 404 → `null`; 503/timeout/empty/malformed → `WeatherProviderException`;
  exception message never contains the API key.

## 14. Acceptance criteria (Gherkin)
```gherkin
Scenario: Forecast payload maps to five days
  Given a canned OpenWeather forecast response with a +00:00 city offset
  When the service fetches the forecast
  Then the result has five days
  And each day has a high, low, precipitation probability and sky condition

Scenario: Unknown city
  Given the provider returns 404
  Then the result is null (no exception)

Scenario: Upstream failure is wrapped
  Given the provider returns 503
  Then WeatherProviderException is thrown
  And its message does not contain the API key
```

## 15. Definition of done
No test performs real I/O; adapter + mapper covered; DI composes with no wiring change.

## 16. Verification script
1. `dotnet test src/Modules/Weather/Tests/Weather.Modules.Weather.Tests --filter Category=OpenWeatherForecast`
2. Confirm tests use a stub `HttpMessageHandler`.

## 17. Out of scope
Geocoding API; caching; provider fallback.

## 18. Risks / notes
Reuse `OpenWeatherOptions.BaseUrl`; the relative URI is `data/2.5/forecast?...`. No new packages.

## 19. Sizing / BCP rationale
4 BCPs — port method + DTO + mapper + adapter + metrics.

## 20. References
- `specs/adr/0017-five-day-forecast.md`
- `specs/epics/e03-weather/e03s02-provider.md`
- `specs/adr/0015-openweather-retry-resilience.md`
