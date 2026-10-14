# Story e03s02 — OpenWeatherMap provider behind IWeatherService

| Field | Value |
|-------|-------|
| ID | e03s02 |
| Epic | e03 Weather Module (M2, core) |
| Status | todo |
| BCPs | 4 |
| Risk | P0 |
| Type | feat |

## 1. Summary
Declare `IWeatherService` in the Weather module's Application layer and implement `OpenWeatherMapService` in its
Infrastructure layer. Map the imperial payload into the domain `WeatherReport`.

## 2. Context
External calls hide behind a module-local port, so the domain is provider-agnostic and tests stay offline.
ADR-0003 documents the endpoint and field semantics.

## 3. User story
As a developer, I want weather fetched behind an interface, so the app is testable and provider-agnostic.

## 4. Business value
Enables offline tests; isolates the provider.

## 5. In scope
- `IWeatherService` port + `WeatherProviderException`.
- `OpenWeatherMapService` typed client; `OpenWeather:BaseUrl`, `OpenWeather:ApiKey`.
- Provider DTOs + `OpenWeatherMapper` → domain; mph → m/s; `Temperature.FromFahrenheit`.
- Timeout (10s) + bounded retry (3).
- Tests with a canned `HttpMessageHandler` (no network).

## 6. Out of scope
Caching; provider fallback.

## 7. Domain concepts
`WeatherReport`, `Temperature`, `WindInfo`, `SkyCondition`.

## 8. Layer touchpoints
| Assembly | Change |
|----------|--------|
| Weather.Application | `Abstractions/IWeatherService`, `WeatherProviderException` |
| Weather.Infrastructure | `OpenWeatherMapService`, options, DTOs, mapper, registration in `WeatherModule` |

## 9. API contract
Internal only.

## 10. Dependencies
e03s01.

## 11. Risks (security: medium — outbound + secret)
| Risk | Mitigation |
|------|-----------|
| API key in logs/errors | Never log the URL; redact `appid` |
| Provider down | Wrap in `WeatherProviderException` → 502 |
| Untrusted JSON | Explicit typed DTOs |

## 12. Implementation steps
1. Add `IWeatherService` + `WeatherProviderException` → verify: `dotnet build Weather.sln --warnaserror`
2. Add provider DTOs + `OpenWeatherMapper` (imperial → domain) → verify: `dotnet test src/Modules/Weather/Tests --filter Category=WeatherMapping`
3. Add `OpenWeatherMapService` with typed client, options, timeout, retry → verify: `dotnet test src/Modules/Weather/Tests --filter Category=WeatherProvider`
4. Add `HttpMessageHandler`-stub tests (success, non-200, malformed JSON, timeout) → verify: `dotnet test src/Modules/Weather/Tests --filter Category=WeatherProvider`
5. Register options + typed client in `WeatherModule` → verify: `dotnet build Weather.sln --warnaserror`

## 13. Test plan
- Mapping: imperial payload → domain.
- Failure: 404/500/timeout/malformed → `WeatherProviderException`.
- Security: API key absent from exception messages.

## 14. Acceptance criteria (Gherkin)
```gherkin
Scenario: Payload maps to domain
  Given a canned imperial response
  When the service fetches weather
  Then temperature.fahrenheit equals the payload value
  And wind speed is converted from mph to m/s

Scenario: Upstream failure is wrapped
  Given the provider returns 503
  Then WeatherProviderException is thrown
  And its message contains no API key
```

## 15. Definition of done
No real I/O in tests; mapper covered.

## 16. Verification script
1. `dotnet test src/Modules/Weather/Tests --filter Category=WeatherProvider`.

## 17. Out of scope
Geocoding API; One Call 3.0.

## 18. Risks / notes
The provider resolves `q={city},{country}`, letting Weather stay module-independent.

## 19. Sizing / BCP rationale
4 BCPs — external boundary + mapping + resilience.

## 20. References
- `specs/adr/0003-weather-provider-and-dew-point.md`
- `specs/tech-architecture/SECURITY_PLAN_LATEST.md`
