# ADR-0015 — Explicit, configurable retry resilience for the OpenWeatherMap client

**Status:** Accepted (user decision 2025-10-16)
**Date:** 2025-10-16
**Relates to:** ADR-0003 (OpenWeatherMap integration), ADR-0007 (Weather module is read-only and self-contained),
e03s02 (typed client), e03s04 (this change)

## Context
The Weather module calls the external OpenWeatherMap (OWM) Current Weather 2.5 endpoint through a typed
`HttpClient`. OWM can be unreachable, slow, or return a transient `5xx`. The project already records the posture
in three places:

- `CONVENTIONS.md` — "External I/O: timeouts, cancellation, **bounded transient retries with jitter**".
- `CONVENTIONS.md` §Defensive Code Categories — **Retry**: transient HTTP failures "bounded, e.g. 3 attempts";
  **Timeout**: outbound weather calls bounded (e.g. 10s); "**Not used** in this project: rate limit, circuit
  breaker".
- `specs/product/SCOPE_LATEST.yaml` — "Rate limiting and circuit breaker for the upstream provider … Deferred —
  single provider, low volume; **timeout + bounded retry suffice**."

The shipped code, however, used `AddStandardResilienceHandler()` (e03s02 task 6). That method applies the
framework's *standard* pipeline — `RateLimiter`, `TotalRequestTimeout`, `Retry`, `CircuitBreaker`, and
`AttemptTimeout` (verified in `Microsoft.Extensions.Http.Resilience` 10.11.0 `HttpStandardResilienceOptions`) —
and its retry count is **not configurable**. Two problems follow:

1. The retry budget cannot be tuned per environment without a code change.
2. The client silently runs a circuit breaker and a rate limiter the project explicitly deferred.

## Decision
Replace `AddStandardResilienceHandler()` with an **explicit resilience pipeline** on the OWM client, containing
only the strategies the project committed to, configured from `appsettings.json`.

| # | Decision | Value |
|---|----------|-------|
| D1 | Pipeline composition | `AddResilienceHandler("openweather", …)`: total timeout (outer) → retry → attempt timeout (inner) |
| D2 | Configuration section | `OpenWeather:Resilience` |
| D3 | Retry count semantics | `MaxRetryAttempts` = retries **after** the first attempt; total attempts = `MaxRetryAttempts + 1` (Polly semantics) |
| D4 | Backoff | Exponential with jitter (`DelayBackoffType.Exponential`, `UseJitter = true`), base `RetryBaseDelaySeconds` |
| D5 | Deferred strategies | No circuit breaker, no rate limiter, no hedging |
| D6 | Startup validation | `[Range]` guards + a cross-field rule `TotalTimeoutSeconds >= AttemptTimeoutSeconds`; `ValidateOnStart()` |
| D7 | Timeout budget | Preserve e03s02's 10s attempt timeout; add a 30s total request cap by default |

Settings and defaults:

```json
"OpenWeather": {
  "Resilience": {
    "MaxRetryAttempts": 3,
    "RetryBaseDelaySeconds": 1,
    "AttemptTimeoutSeconds": 10,
    "TotalTimeoutSeconds": 30
  }
}
```

`HttpRetryStrategyOptions` handles only transient failures by default — `408`, `429`, `5xx`, and
`HttpRequestException`/`TimeoutRejectedException` — so a `404` (city not found) is never retried and remains the
expected `null` outcome. The final failure still maps to `WeatherProviderException` → `502
DEPENDENCY_UNAVAILABLE` (ADR-0011).

## Rationale
- **Configurability without code changes.** Operations can widen or narrow the retry budget per environment,
  which is what the change request asked for.
- **Match the documented posture.** Removing the circuit breaker and rate limiter makes the running system agree
  with `SCOPE_LATEST.yaml` and `CONVENTIONS.md`; both can be re-introduced later behind their own ADR.
- **Explicit is auditable.** An explicit pipeline states the order and the values in one place, instead of
  inheriting framework defaults that change between package versions.
- **Preserves existing contracts.** Defaults keep the 10s attempt timeout from e03s02; the public
  `IWeatherProvider` port, the mapper, and the 502 mapping are untouched.

## Consequences
- **Positive:** operator-tunable resilience; no unintended throttling/circuit-breaking; retry behavior is
  provably configurable and unit-tested via HTTP call counts.
- **Negative:** more wiring code than one `AddStandardResilienceHandler()` call, and we now own timeout/retry
  defaults that the framework previously maintained.
- **Neutral:** no new instrumentation; retries are invisible to the existing `weather.provider.*` metrics, which
  continue to observe the final outcome only.
- **Risk:** request amplification if an operator sets a high `MaxRetryAttempts`; bounded by the `[Range]` (≤ 10)
  and the total timeout.

## Alternatives rejected
- **Keep `AddStandardResilienceHandler()` and bind `HttpStandardResilienceOptions.Retry` from configuration.**
  Smaller diff, but keeps the deferred circuit breaker and rate limiter and couples us to the framework's
  standard-pipeline defaults. Rejected.
- **No total request timeout.** A 3-retry budget with a 10s attempt timeout can run ~40s; a total cap bounds the
  worst case. Rejected.
- **Retry on any non-success status.** A `404` is an expected outcome and must not consume the budget. Rejected;
  the strategy's transient-only default is kept.
- **Add `weather.provider.retries` telemetry now.** Requires an `OBSERVABILITY_PLAN_LATEST.md` update first;
  deferred as out of scope for this change.

## Evidence — what the package documents
| Source | Statement used |
|--------|----------------|
| `Microsoft.Extensions.Http.Resilience` 10.11.0 `HttpStandardResilienceOptions` | Exposes `RateLimiter`, `TotalRequestTimeout`, `Retry`, `CircuitBreaker`, `AttemptTimeout` — i.e. the standard handler is broader than retry + timeout. |
| `Microsoft.Extensions.Http.Resilience.HttpRetryStrategyOptions` | "By default, the options are configured to handle only transient failures." |
| `Microsoft.Extensions.DependencyInjection.ResilienceHttpClientBuilderExtensions.AddResilienceHandler` | Accepts a `ResiliencePipelineBuilder<HttpResponseMessage>` and a `ResilienceHandlerContext` (used to read `IOptions<OpenWeatherResilienceOptions>`). |
| `Polly.Retry.RetryStrategyOptions<T>.MaxRetryAttempts` | Number of retries after the initial attempt. |
