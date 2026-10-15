# Story e14s01 — Forecast domain: intervals and city-local daily aggregation

| Field | Value |
|-------|-------|
| ID | e14s01 |
| Epic | e14 Five-Day Forecast (M2, core) |
| Status | planned |
| BCPs | 5 |
| Risk | P0 |
| Type | feat |
| Delta | ADDED |

## 1. Summary
Add a Forecast sub-domain: `ForecastInterval` (one provider step), `PrecipitationProbability` (0-100),
`DailyForecast`, and `WeatherForecast` whose `FromIntervals` factory groups 3-hour intervals into
city-local calendar days and derives high, low, precipitation probability, and dominant sky condition.

## 2. Context
OpenWeather's 5 day / 3 hour endpoint returns 3-hour steps, not days. Aggregation is a domain concern per
ADR-0003 (conversion/derivation live in the domain). Grouping uses `city.timezone` (seconds from UTC)
supplied by the provider adapter.

## 3. User story
As a user, I want each forecast card to show one day's high, low, rain chance, and sky, so the strip is
readable at a glance.

## 4. Business value
Turns 40 raw provider steps into the five daily cards the design shows. Offline-testable, no new dependency.

## 5. In scope
- `PrecipitationProbability` value object (`FromRatio(double)` 0..1, `FromPercent(int)` 0..100, `Percent`).
- `ForecastInterval` record (UTC instant, temperature, precipitation probability, sky condition) + factory guards.
- `DailyForecast` record (DateOnly date, high, low, precipitation probability, sky condition).
- `WeatherForecast` record + `FromIntervals(location, intervals, utcOffset)`:
  group by local calendar day, order ascending, high = max temp, low = min temp,
  precipitation = max percent, sky = most frequent `SkyCondition` (tie-break: first occurrence).
- Domain tests for every rule above.

## 6. Out of scope
Provider adapter (e14s02); DTO/query/endpoint (e14s03); UI (e14s04); capping the day count (e14s03).

## 7. Domain concepts
`ForecastInterval` → `DailyForecast` → `WeatherForecast`; reuses `Temperature`, `SkyCondition`, `Location`.

## 8. Layer touchpoints
| Assembly | Change |
|----------|--------|
| `Weather.Modules.Weather.Domain` | new `Forecasts/` folder |
| `Weather.Modules.Weather.Tests` | new domain tests |

## 9. API contract
Internal only.

## 10. Dependencies
None (extends e03s01 value objects).

## 11. Risks
| Risk | Mitigation |
|------|-----------|
| Midnight / offset boundaries split a day wrongly | Group on `interval.AtUtc.ToOffset(utcOffset).Date`; boundary tests |
| Non-deterministic condition choice | Frequency with first-occurrence tie-break; asserted in tests |
| Offset out of range throws | Validate `abs(utcOffset) <= 14h` with `WeatherDomainException` |

## 12. Implementation steps
1. Add `PrecipitationProbability` → verify: `dotnet test src/Modules/Weather/Tests/Weather.Modules.Weather.Tests --filter Category=ForecastDomain`
2. Add `ForecastInterval` + `DailyForecast` → verify: `dotnet test src/Modules/Weather/Tests/Weather.Modules.Weather.Tests --filter Category=ForecastDomain`
3. Add `WeatherForecast.FromIntervals` aggregation + guards → verify: `dotnet test src/Modules/Weather/Tests/Weather.Modules.Weather.Tests --filter Category=ForecastAggregation`
4. Story gate: full preflight + layout check → verify: `dotnet build Weather.sln --warnaserror && dotnet format Weather.sln --verify-no-changes && dotnet test Weather.sln && bash scripts/check-layout.sh`

## 13. Test plan
- `PrecipitationProbability`: 0, 1, 0.5 → 50%, clamping/guard on out-of-range.
- Aggregation: two intervals on the same local day → high/low/max-pop; intervals either side of local midnight
  → two days; dominant condition; ordering ascending; empty intervals → `WeatherDomainException`.

## 14. Acceptance criteria (Gherkin)
```gherkin
Scenario: Daily high and low
  Given intervals at 06:00 and 15:00 local with temperatures 8C and 14C
  When the forecast is built
  Then one day has high 14C and low 8C

Scenario: City-local day boundary
  Given an interval at 23:30 UTC and one at 00:30 UTC with a +02:00 city offset
  Then they fall on the same local day

Scenario: Precipitation probability
  Given a day whose intervals have pop 0.2 and 0.75
  Then the day's precipitation probability is 75
```

## 15. Definition of done
Aggregation rules covered by offline domain tests; no Infrastructure/ASP.NET reference added to Domain.

## 16. Verification script
1. `dotnet test src/Modules/Weather/Tests/Weather.Modules.Weather.Tests --filter Category=ForecastDomain`
2. `dotnet test src/Modules/Weather/Tests/Weather.Modules.Weather.Tests --filter Category=ForecastAggregation`

## 17. Out of scope
Provider parsing; endpoint; UI.

## 18. Risks / notes
Keep `decimal` for temperatures (DECISIONS Q2). Use `DateOnly` for the day key.

## 19. Sizing / BCP rationale
5 BCPs — value object + two records + aggregation rules + guards.

## 20. References
- `specs/adr/0017-five-day-forecast.md`
- `specs/epics/e03-weather/DECISIONS.md`
