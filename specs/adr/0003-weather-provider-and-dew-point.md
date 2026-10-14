# ADR-0003 — OpenWeatherMap integration and dew-point derivation

**Status:** Proposed (needs user confirmation)
**Date:** 2025-10-13

## Context
Weather must be proxied behind `IWeatherProvider`. The payload must include location, UTC time,
wind (speed + direction), visibility, pressure, sky condition, temperature in F and C, dew point,
and relative humidity. The requirement states Fahrenheit arrives from the provider and is converted
to Celsius.

## Provider facts (verified against OpenWeatherMap docs)
- Current Weather API: `GET https://api.openweathermap.org/data/2.5/weather?q={city},{country}&units=imperial&appid={key}`.
- With `units=imperial`, `main.temp` is in **Fahrenheit**, `wind.speed` in **miles/hour**, `visibility` in meters.
- The basic Current Weather response does **not** include `dew_point` (that field exists in One Call 3.0).
- The response includes `main.humidity` (relative humidity %) and `weather[0].main` (sky condition).

## Decision
- Use the **Current Weather 2.5** endpoint with `units=imperial`.
- Convert Fahrenheit → Celsius in the domain (`Temperature.FromFahrenheit`).
- **Derive dew point** from temperature and relative humidity using the **Magnus formula**, in the domain.
- Convert wind speed from mph to m/s at the infrastructure boundary; keep the domain in SI.

## Rationale
- The 2.5 endpoint works with a free API key; One Call 3.0 requires a separate subscription.
- Deriving dew point keeps the calculation in the domain where it is unit-testable and offline.
- SI inside the domain avoids unit leakage into business rules.

## Consequences
- Positive: offline-testable dew point; one API key; no paid tier.
- Negative: derived dew point is an approximation (accurate for RH > 50%, acceptable for this scope).
- Risk: provider field extraction (e.g. `wind.speed`) is unit-tested with canned JSON.

## Alternatives rejected
- **One Call 3.0:** returns `dew_point` directly but needs a separate subscription — rejected for scope.
