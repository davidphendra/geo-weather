# Story e03s04 — Configurable retry resilience for the OpenWeatherMap client

| Field | Value |
|-------|-------|
| ID | e03s04 |
| Epic | e03 Weather Module (M2, core) |
| Status | todo |
| BCPs | 3 |
| Risk | P1 |
| Type | feat |
| Delta | MODIFIED + ADDED + REMOVED (see §5a) |

## 1. Summary
Make the outbound OpenWeatherMap retry policy **configurable from `appsettings.json`** and replace the
over-broad `AddStandardResilienceHandler()` with an explicit resilience pipeline that contains only
**total timeout → retry → attempt timeout**. The retry count, backoff base delay, and both timeouts are bound
from the new `OpenWeather:Resilience` section and validated at startup.

## 2. Context
The OpenWeatherMap API is an external dependency that can be unreachable, slow, or return a transient 5xx.
`CONVENTIONS.md` already mandates "bounded transient retries with jitter", but the shipped implementation used
`AddStandardResilienceHandler()`, which is **not configurable** and additionally turns on a **circuit breaker and
a rate limiter** that `specs/product/SCOPE_LATEST.yaml` explicitly defers ("single provider, low volume; timeout +
bounded retry suffice"). This story closes that gap: the policy becomes operator-configurable and matches the
documented posture. The provider stays behind `IWeatherProvider` (ADR-0003, ADR-0007) — no interface changes.

> **Discovery note (D4/DD4):** `Microsoft.Extensions.Http.Resilience` `10.11.0` exposes
> `HttpStandardResilienceOptions` with `RateLimiter`, `TotalRequestTimeout`, `Retry`, `CircuitBreaker`, and
> `AttemptTimeout`; `AddResilienceHandler` accepts a `ResiliencePipelineBuilder<HttpResponseMessage>` and
> `HttpRetryStrategyOptions` defaults to handling only transient failures (408, 429, 5xx, `HttpRequestException`).

## 3. User story
As an operator, I want the OpenWeatherMap retry policy driven by configuration, so I can tune resilience per
environment without a code change, and so we do not silently run strategies the project deferred.

## 4. Business value
Fewer failed weather lookups during transient upstream blips, with an operator-tunable budget. Also removes an
undocumented circuit breaker/rate limiter that could reject requests under load.

## 5a. Requirement deltas

#### MODIFIED: Retry policy for `IWeatherProvider` calls is configurable
**Before:** `AddStandardResilienceHandler()` applied a fixed standard pipeline (retry = 3 attempts) plus an
implicit circuit breaker and rate limiter. No values were configurable from configuration.
**After:** An explicit pipeline `total timeout → retry → attempt timeout` configured from
`OpenWeather:Resilience`. Retries are exponential with jitter and bounded by `MaxRetryAttempts`
(retries *after* the first attempt; total attempts = `MaxRetryAttempts + 1`).

#### ADDED: `OpenWeather:Resilience` configuration section
Full requirement: bind and validate `MaxRetryAttempts`, `RetryBaseDelaySeconds`, `AttemptTimeoutSeconds`, and
`TotalTimeoutSeconds`; fail fast at startup when the values are out of range or inconsistent
(`TotalTimeoutSeconds >= AttemptTimeoutSeconds`).

#### REMOVED: Implicit circuit breaker and rate limiter on the OpenWeatherMap client
**Before:** `AddStandardResilienceHandler()` included `CircuitBreaker` and `RateLimiter` strategies.
**After:** (removed) — both remain deferred by `SCOPE_LATEST.yaml`; the explicit pipeline contains only timeouts
and retry. Rationale: a single, low-volume provider; a circuit breaker would reject traffic and a rate limiter
would throttle without a stated need.

## 5. In scope
- `OpenWeatherResilienceOptions` (`SectionName = "OpenWeather:Resilience"`): `MaxRetryAttempts` (int, default 3),
  `RetryBaseDelaySeconds` (double, default 1), `AttemptTimeoutSeconds` (double, default 10),
  `TotalTimeoutSeconds` (double, default 30); `[Range]` guards on each value.
- `OpenWeatherResilienceOptionsValidator` (`IValidateOptions<>`) enforcing the cross-field invariant
  `TotalTimeoutSeconds >= AttemptTimeoutSeconds`.
- `AddWeatherModule`: bind + `ValidateDataAnnotations()` + `ValidateOnStart()`; replace
  `.AddStandardResilienceHandler()` with `.AddResilienceHandler("openweather", …)`.
- `OpenWeather:Resilience` defaults in `src/Weather.WebUI/appsettings.json`.
- Unit tests with a counting stub `HttpMessageHandler` asserting call counts; options-validator tests; a host
  test proving the shipped defaults bind.

## 6. Out of scope
- Circuit breaker, rate limiting, hedging (still deferred by `SCOPE_LATEST.yaml`).
- A `weather.provider.retries` metric (would require an `OBSERVABILITY_PLAN_LATEST.md` update first).
- Changes to `IWeatherProvider`, `OpenWeatherMapService`, the mapper, or the 502 error mapping.
- Retrying non-GET methods (the client only issues GET).

## 7. Domain concepts
None. Infrastructure-only (`OpenWeatherOptions`, `IWeatherProvider`), so the Domain is untouched.

## 8. Layer touchpoints
| Assembly | Change |
|----------|--------|
| `Weather.Modules.Weather.Domain` | none |
| `Weather.Modules.Weather.Application` | none |
| `Weather.Modules.Weather.Infrastructure` | `OpenWeatherMap/OpenWeatherResilienceOptions.cs`, `OpenWeatherMap/OpenWeatherResilienceOptionsValidator.cs`, `WeatherModuleExtensions.cs` |
| `Weather.WebUI` | `appsettings.json` (new `OpenWeather:Resilience` block) |
| `Weather.Modules.Weather.Tests`, `Weather.IntegrationTests` | new tests (see §13) |

## 9. API contract
No public API change. New configuration contract:
```json
"OpenWeather": {
  "BaseUrl": "https://api.openweathermap.org/",
  "Resilience": {
    "MaxRetryAttempts": 3,
    "RetryBaseDelaySeconds": 1,
    "AttemptTimeoutSeconds": 10,
    "TotalTimeoutSeconds": 30
  }
}
```
Transient upstream failures (`408`, `429`, `5xx`, transport exception) are retried up to
`MaxRetryAttempts`; the final failure still surfaces as `WeatherProviderException` → `502 DEPENDENCY_UNAVAILABLE`.
`404` stays an expected outcome (`null`) and is **never** retried.

## 10. Dependencies
e03s02 (the typed client), e09 (the 502 mapping stays unchanged). No new module dependency.

## 11. Risks
| Risk | Mitigation |
|------|-----------|
| Retry storms / request amplification | `MaxRetryAttempts` bounded (≤ 10) + `TotalTimeoutSeconds` cap; exponential backoff with jitter |
| Tests become slow or flaky | Tests set a sub-second `RetryBaseDelaySeconds`; call counts asserted, not wall-clock |
| Retry logs leak the API key (key is in the query string) | `AddResilienceHandler` does not enable Polly telemetry by default; never log the URI (DD6) |
| Timeout interaction starvation | Startup validation enforces `TotalTimeoutSeconds >= AttemptTimeoutSeconds` |
| Removing the standard handler changes timeout semantics | Preserve today's defaults (10s attempt, 30s total) and keep the existing `WeatherProviderException` mapping |

## 12. Implementation steps
1. Add `OpenWeatherResilienceOptions` + `OpenWeatherResilienceOptionsValidator` → verify: `dotnet test src/Modules/Weather/Tests/Weather.Modules.Weather.Tests --filter Category=OpenWeatherResilienceOptions`
2. Bind + validate the options and replace `.AddStandardResilienceHandler()` with the explicit `total → retry → attempt` pipeline in `AddWeatherModule` → verify: `dotnet test src/Modules/Weather/Tests/Weather.Modules.Weather.Tests --filter Category=WeatherServiceWiring`
3. Add behavioral retry tests (transient retried N times, persistent exhausted, zero retries, 404 not retried, transport retried) → verify: `dotnet test src/Modules/Weather/Tests/Weather.Modules.Weather.Tests --filter Category=OpenWeatherResilience`
4. Add `OpenWeather:Resilience` defaults to `src/Weather.WebUI/appsettings.json` and a host test asserting they bind → verify: `dotnet test src/Tests/Weather.IntegrationTests --filter Category=WeatherResilienceConfig`
5. Update specs (`ADR-0015`, `TEST_PLAN_LATEST.md`, `tech-stack.md`, `SCOPE_LATEST.yaml`, `CONVENTIONS.md`, `DECISIONS.md`) → verify: `rg -n "e03s04|ADR-0015|MaxRetryAttempts" specs CONVENTIONS.md`
6. Standards + full preflight → verify: `dotnet build Weather.sln --warnaserror && dotnet format Weather.sln --verify-no-changes && dotnet test Weather.sln`

## 13. Test plan
| Scenario ID | Description | Level | Risk |
|-------------|-------------|-------|------|
| SC-e03s04-P0-01 | 5xx,5xx,200 with `MaxRetryAttempts=2` → 3 HTTP calls and a report | Unit (DI) | P0 |
| SC-e03s04-P0-02 | Persistent 5xx with `MaxRetryAttempts=2` → 3 calls, `WeatherProviderException` | Unit (DI) | P0 |
| SC-e03s04-P1-01 | `MaxRetryAttempts=0` → 1 call, `WeatherProviderException` | Unit (DI) | P1 |
| SC-e03s04-P1-02 | `404` → 1 call, returns `null` (never retried) | Unit (DI) | P1 |
| SC-e03s04-P1-03 | Transport `HttpRequestException` ×2 then 200 → 3 calls | Unit (DI) | P1 |
| SC-e03s04-P2-01 | Out-of-range / inconsistent options fail validation at startup | Unit | P2 |
| SC-e03s04-P2-02 | Shipped `appsettings.json` binds the documented defaults | Integration (host) | P2 |

## 14. Acceptance criteria (Gherkin)
```gherkin
Scenario: Transient failure is retried then succeeds
  Given the provider is configured with MaxRetryAttempts = 2
  And the upstream returns 503, then 503, then 200
  When current weather is fetched
  Then the provider was called 3 times
  And a WeatherReport is returned

Scenario: Persistent failure exhausts the retry budget
  Given MaxRetryAttempts = 2
  And the upstream always returns 503
  When current weather is fetched
  Then the provider was called 3 times
  And a WeatherProviderException is thrown

Scenario: Retries can be disabled
  Given MaxRetryAttempts = 0
  And the upstream always returns 503
  When current weather is fetched
  Then the provider was called exactly 1 time

Scenario: Not-found is not retried
  Given the upstream returns 404
  When current weather is fetched
  Then the provider was called exactly 1 time
  And the result is null

Scenario: Invalid configuration fails fast
  Given TotalTimeoutSeconds is less than AttemptTimeoutSeconds
  When the options are validated
  Then validation fails with a message naming both settings
```

## 15. Definition of done
Tasks green; retry count provably drives HTTP call count; the standard handler and its circuit breaker/rate
limiter are gone; defaults preserve the 10s attempt / 30s total timeout; `WeatherProviderException` mapping is
unchanged; no live network in tests.

## 16. Verification script
1. `dotnet test src/Modules/Weather/Tests/Weather.Modules.Weather.Tests --filter Category=OpenWeatherResilience`
2. `dotnet test src/Tests/Weather.IntegrationTests --filter Category=WeatherResilienceConfig`
3. Set `OpenWeather:Resilience:MaxRetryAttempts=0` in `appsettings.Development.json`, run the host with a bad
   key, and confirm a single upstream attempt (log `Category=OpenWeatherResilience`), then revert.
4. `dotnet build Weather.sln --warnaserror && dotnet format Weather.sln --verify-no-changes && dotnet test Weather.sln`

## 17. Out of scope
Caching; circuit breaker; provider fallback; new telemetry instruments.

## 18. Risks / notes
`MaxRetryAttempts` follows Polly semantics (retries after the first attempt), so `MaxRetryAttempts=3` means up to
four total attempts. Document this wherever the setting is described so operators do not misread it.
Implementation note (discovered during TDD): Polly rejects `MaxRetryAttempts < 1`, so `0` is implemented by not
registering a retry strategy at all — worth a comment in the composition root and a test.

## 19. Sizing / BCP rationale
3 BCPs — options + validator, composition-root pipeline, and the behavioral test harness that proves the
configuration drives call counts.

## 20. References
- `specs/adr/0015-openweather-retry-resilience.md`
- `specs/adr/0003-weather-provider-and-dew-point.md`
- `specs/adr/0007-weather-module-read-only-self-contained.md`
- `specs/product/SCOPE_LATEST.yaml` (rate limiting / circuit breaker deferred)
- `CONVENTIONS.md` §Defensive Code Categories
- `specs/tech-architecture/SECURITY_PLAN_LATEST.md`
