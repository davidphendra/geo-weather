# Story e03s02 — IWeatherProvider port + OpenWeatherMap adapter

| Field | Value |
|-------|-------|
| ID | e03s02 |
| Epic | e03 Weather Module (M2, core) |
| Status | todo |
| BCPs | 4 |
| Risk | P0 |
| Type | feat |

## 1. Summary
Declare `IWeatherProvider` in the Weather module's Application layer and implement `OpenWeatherMapService` in
its Infrastructure layer: typed `HttpClient`, imperial payload, F→C via the domain, mph→m/s, UTC time, and an
API-key `DelegatingHandler` that keeps the key out of logs and exception messages.

## 2. Context
External calls hide behind a module-local port, so the domain is provider-agnostic and tests stay offline
(ADR-0003). The provider geocodes `q={city},{country}`, so Weather needs no other module.

## 3. User story
As a developer, I want weather fetched behind an interface, so the app is testable offline and provider-agnostic.

## 4. Business value
Enables offline tests and isolates the provider; keeps the API key out of logs.

## 5. In scope
- `IWeatherProvider` port (`Task<WeatherReport?>`, `null` = city not found) + `WeatherProviderException` (Application/Abstractions).
- `OpenWeatherOptions` (`OpenWeather:BaseUrl`, `OpenWeather:ApiKey`).
- Provider DTOs + `OpenWeatherMapMapper` (imperial → domain).
- `OpenWeatherMapService` typed client; 10s timeout.
- `OpenWeatherApiKeyHandler` (`DelegatingHandler`) appending `appid`, plus the named client's log category set to `Warning` so the final URI (which carries the key) is never logged.
- `AddWeatherModule` composition root (options, typed client, MediatR, validators, pipeline).
- Tests with a canned `HttpMessageHandler` (no network).

## 6. Out of scope
Caching; provider fallback; One Call 3.0; query/endpoint (e03s03).

## 7. Domain concepts
`WeatherReport`, `Temperature`, `WindInfo`, `SkyCondition`, `RelativeHumidity`, `DewPoint`, `Visibility`, `Pressure`, `Location`.

## 8. Layer touchpoints
| Assembly | Change |
|----------|--------|
| `Weather.Modules.Weather.Application` | `Abstractions/IWeatherProvider`, `WeatherProviderException` |
| `Weather.Modules.Weather.Infrastructure` | `OpenWeatherMap/` options + DTOs + mapper + service + key handler; `WeatherModuleExtensions` |

## 9. API contract
Internal only.

## 10. Dependencies
e03s01.

## 11. Risks (security: medium — outbound + secret)
| Risk | Mitigation |
|------|-----------|
| API key in logs/exceptions | Attach via `DelegatingHandler`; set `Logging:LogLevel:System.Net.Http.HttpClient.Weather = Warning`; never log the URL |
| Provider down | Catch `HttpRequestException` → `WeatherProviderException` → 502 |
| Untrusted JSON | Explicit typed DTOs, no polymorphic deserialization |
| Malformed/empty body | `WeatherProviderException` |

## 12. Implementation steps
1. Add `IWeatherProvider` (nullable result = not found) + `WeatherProviderException` → verify: `dotnet build src/Modules/Weather/Weather.Modules.Weather.Application/Weather.Modules.Weather.Application.csproj --warnaserror`
2. Add `OpenWeatherOptions` + provider DTOs + `OpenWeatherMapMapper` → verify: `dotnet test src/Modules/Weather/Tests/Weather.Modules.Weather.Tests --filter Category=OpenWeatherMapping`
3. Add `OpenWeatherMapService` typed client + 10s timeout → verify: `dotnet test src/Modules/Weather/Tests/Weather.Modules.Weather.Tests --filter Category=OpenWeatherMapService`
4. Add `OpenWeatherApiKeyHandler` + the per-client `Warning` log level; assert the key never appears in exception messages or log output → verify: `dotnet test src/Modules/Weather/Tests/Weather.Modules.Weather.Tests --filter Category=ApiKeyHandler`
5. Add `AddWeatherModule` (options, typed client, MediatR, FluentValidation, pipeline) → verify: `dotnet test src/Modules/Weather/Tests/Weather.Modules.Weather.Tests --filter Category=WeatherServiceWiring`

## 13. Test plan
- Mapping: canned imperial payload → domain (temperature F, mph→m/s, pressure hPa, humidity, sky normalized, UTC time).
- Service: 200 → `WeatherReport`; 404 → `null` (expected, no exception); 5xx/timeout/empty/malformed → `WeatherProviderException`.
- Security: assert the API key never appears in exception messages.

## 14. Acceptance criteria (Gherkin)
```gherkin
Scenario: Provider payload maps to domain
  Given a canned OpenWeatherMap imperial response
  When the service fetches weather
  Then temperature.fahrenheit equals the payload value
  And wind speed is converted from mph to m/s
  And the sky condition is normalized

Scenario: Upstream failure is wrapped
  Given the provider returns 503
  When the service fetches weather
  Then WeatherProviderException is thrown
  And its message does not contain the API key

Scenario: Unknown city
  Given the provider returns 404
  Then the result is null (no exception)
```

## 15. Definition of done
No test performs real I/O; mapper + service + key handler covered; DI composes.

## 16. Verification script
1. `dotnet test src/Modules/Weather/Tests/Weather.Modules.Weather.Tests --filter Category=OpenWeather`.
2. Confirm tests use a stub `HttpMessageHandler` (no sockets).

## 17. Out of scope
Geocoding API; caching.

## 18. Risks / notes
Packages added here (central package management): MediatR, FluentValidation, Microsoft.Extensions.Http,
Microsoft.Extensions.Options.ConfigurationExtensions, Microsoft.Extensions.Configuration.Abstractions.

## 19. Sizing / BCP rationale
4 BCPs — external boundary + mapping + resilience + secret handling.

## 20. References
- `specs/adr/0003-weather-provider-and-dew-point.md`
- `specs/adr/0007-weather-module-read-only-self-contained.md`
- `specs/tech-architecture/SECURITY_PLAN_LATEST.md`
