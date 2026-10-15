# ADR-0017 — Five-day forecast (OpenWeather 5 day / 3 hour forecast)

**Status:** Accepted (D1-D6 confirmed 2026-10-15)
**Date:** 2026-10-15

## Context
The Weather Explorer page has a "5-Day Predictive Ensemble Forecast" section that is deliberately empty
(`ForecastStrip` renders "Forecast not available — current conditions only", per e06s03 / UI_DESIGN_PLAN D3).
`SCOPE_LATEST.yaml` lists forecast as out of scope. The user now wants that section populated.

The Weather module already proxies OpenWeatherMap behind `IWeatherProvider`, with Fahrenheit→Celsius and
dew-point derivation owned by the domain (ADR-0003), imperial units at the infrastructure boundary, and
offline tests via canned JSON.

## Provider facts (verified against OpenWeather docs, 2026-10-15)
- 5 day / 3 hour forecast: `GET https://api.openweathermap.org/data/2.5/forecast?q={city},{country}&units=imperial&appid={key}`.
- The response is a **3-hour step** series: `list[]` (40 entries = 5 days) plus a `city` block.
- With `units=imperial`: `list.main.temp` / `temp_min` / `temp_max` / `feels_like` are **Fahrenheit**,
  `list.wind.speed` / `gust` are **miles/hour**, `list.main.pressure` is hPa.
- Per-entry fields: `list.dt` (unix UTC), `list.main.temp`, `list.main.temp_min`/`temp_max` (optional,
  "at the moment of calculation"), `list.main.humidity`, `list.weather[0].main`, `list.wind.speed`,
  `list.wind.deg`, `list.visibility` (metres), `list.pop` (**probability of precipitation, 0..1**),
  `list.rain.3h` / `list.snow.3h` (mm), `list.dt_txt` (ISO UTC).
- The `city` block carries `city.name`, `city.country`, `city.timezone` (**shift in seconds from UTC**),
  `city.sunrise` / `city.sunset`.
- The endpoint is part of the free tier and uses the same API key as Current Weather 2.5.

## Decision
1. **Consume `GET /data/2.5/forecast` with `units=imperial`** through the existing typed `HttpClient`,
   key `DelegatingHandler`, and `OpenWeather:Resilience` pipeline. No new configuration, no new key.
2. **Aggregate the 3-hour series into calendar days in the domain.** The provider adapter parses `list[]`
   into domain `ForecastInterval` values (timestamp + temperature + precipitation probability + sky
   condition). The domain groups intervals by **city-local calendar day** using `city.timezone`, and derives
   each `DailyForecast`:
   - `High` = max `main.temp` across the day's intervals,
   - `Low` = min `main.temp` across the day's intervals,
   - `PrecipitationProbability` = max `list.pop` (ratio → percent),
   - `SkyCondition` = most frequent `weather[0].main` of the day (tie-break: first occurrence).
   Rationale: deriving in the domain mirrors the dew-point decision (ADR-0003) and keeps aggregation
   offline-testable.
3. **Exactly five day cards.** The 120-hour window can touch six local calendar days; the API returns the
   first five days, today counting as day 1. The truncation lives in the application DTO mapper so the
   domain stays provider-agnostic.
4. **Reuse `IWeatherProvider` / `IWeatherService`** by adding a second method (`GetForecastAsync`) instead of
   introducing a parallel port. One provider, one composition root, one key handler.
5. **New endpoint `GET /api/weather/{cityName}/forecast?countryCode=`** returning `WeatherForecastDto`.
   `GET /api/weather/{cityName}` is unchanged. Two calls per city selection is acceptable at this volume and
   keeps both contracts single-purpose.
6. **Failure semantics follow ADR-0011 / ADR-0003:** unknown city → `404 RESOURCE_NOT_FOUND`; provider
   failure/malformed body → `502 DEPENDENCY_UNAVAILABLE`; invalid input → `422 VALIDATION_FAILED`. On the UI,
   a forecast failure must **not** blank current conditions; the strip shows an explicit unavailable state.
7. **Metrics:** add `weather.forecast.requests` (Counter) and `weather.forecast.duration` (Histogram) to the
   shared meter in `Weather.BuildingBlocks.Application` and to the observability catalog.

## Rationale
- Free tier, same key, no subscription (unlike One Call 3.0, already rejected in ADR-0003).
- Daily aggregation in the domain keeps provider quirks (3-hour steps, optional temp_min/max, ratio pop) out
  of the application and UI layers.
- City-local day boundaries match what a user means by "Thursday" and use the offset the provider supplies.

## Consequences
- Positive: the design's forecast strip becomes real with no fabricated data; domain aggregation is
  deterministic and offline-testable; no new dependency or secret.
- Negative: two upstream calls per city selection; a derived "dominant condition" is an approximation of the
  day.
- Risk: `main.temp_min`/`temp_max` are documented as optional and "at the moment of calculation"; the plan
  deliberately derives high/low from `main.temp` across the day instead, and is covered by canned-JSON tests.

## Alternatives rejected
- **Combine current + forecast in one endpoint** (e.g. `?include=forecast`): changes the stable current
  contract and couples two failure modes.
- **Return the raw 40 3-hour slots and aggregate in the UI:** pushes provider shape and unit conversion into
  the presentation layer, violating the domain-owns-conversion rule.
- **One Call 3.0** (`dew_point`, hourly/daily summaries): requires a separate subscription (ADR-0003).
- **New `IForecastProvider` port:** duplicates the typed client, key handler, and resilience wiring for no
  isolation benefit.

## Confirmed decisions
| # | Decision | Choice |
|---|----------|--------|
| D1 | Day boundary | City-local via `city.timezone` |
| D2 | Day count | First 5 days (today = day 1) |
| D3 | Endpoint | New `GET /api/weather/{cityName}/forecast` |
| D4 | Port | Extend `IWeatherProvider` / `IWeatherService` |
| D5 | UI partial failure | Keep current conditions; strip shows unavailable |
| D6 | Metrics | Add `weather.forecast.*` instruments + catalog entry |

## References
- `specs/adr/0003-weather-provider-and-dew-point.md`
- `specs/adr/0011-rest-api-response-contract.md`
- `specs/adr/0015-openweather-retry-resilience.md`
- `specs/tech-architecture/UI_DESIGN_PLAN_LATEST.md` (D3, component list)
- `specs/tech-architecture/OBSERVABILITY_PLAN_LATEST.md` (metric catalog)
- OpenWeather 5 day / 3 hour forecast: `https://openweathermap.org/api/forecast5`
