# Story e03s01 — Weather domain: value objects + WeatherReport

| Field | Value |
|-------|-------|
| ID | e03s01 |
| Epic | e03 Weather Module (M2, core) |
| Status | todo |
| BCPs | 5 |
| Risk | P0 |
| Type | feat |

## 1. Summary
Build the Weather module's framework-free domain as immutable C# records: `Temperature` (F↔C),
`RelativeHumidity`, `DewPoint` (Magnus), `WindInfo` (+ cardinal), `SkyCondition`, `Visibility`, `Pressure`,
`Location`, and the `WeatherReport` model with a `Create` factory.

## 2. Context
This is the core domain of the application and the only real business logic. Fahrenheit arrives from the
provider and is converted here; dew point is derived here. The module persists nothing, so there is no
aggregate root and no identity — value objects plus one immutable report.

## 3. User story
As a developer, I want a tested, self-contained weather domain, so metric math is correct and independent of
the provider and of any shared BuildingBlocks.

## 4. Business value
Guarantees correctness of every metric shown to users; keeps the core domain free of framework and cross-module
dependencies.

## 5. In scope
- `Temperature`: `FromFahrenheit`, `FromCelsius`, `Celsius`, `Fahrenheit`, 1-decimal rounding, record equality; `decimal` throughout.
- `RelativeHumidity` (0–100 guard) and `DewPoint.Derive(Temperature, RelativeHumidity)` via Magnus, **clamped to `min(derived, temperature)`** so `dewPoint ≤ temperature` always holds.
- `WindInfo` (m/s, **nullable** direction 0–360, `Cardinal` → `"Variable"` when direction is absent) + `CardinalDirection.FromDegrees`.
- `SkyCondition` (typesafe instances + `FromProvider`), `Visibility`, `Pressure`, `Location`.
- `WeatherReport` immutable record with `Create` (UTC guard; computes dew point).
- `WeatherDomainException` (module-local).
- Domain unit tests.

## 6. Out of scope
Provider mapping (e03s02); query/endpoint (e03s03); persistence; BuildingBlocks; domain events.

## 7. Domain concepts
`WeatherReport`, `Temperature`, `RelativeHumidity`, `DewPoint`, `WindInfo`, `CardinalDirection`,
`SkyCondition`, `Visibility`, `Pressure`, `Location`.

## 8. Layer touchpoints
| Assembly | Change |
|----------|--------|
| `Weather.Modules.Weather.Domain` | `WeatherReports/` value objects + `WeatherReport`; `WeatherDomainException` |

No other assembly changes. Domain references BCL only.

## 9. API contract
None (pure domain).

## 10. Dependencies
e00s01 (the Weather module scaffold exists). No BuildingBlocks.

## 11. Risks
| Risk | Mitigation |
|------|-----------|
| Floating-point drift in conversion | `decimal` + single 1-decimal rounding rule; tested |
| −40 equilibrium mishandled | explicit P0 test at −40 |
| VOs drift into anemic records | private constructors + factory guards |

## 12. Implementation steps
1. Add `Temperature` → verify: `dotnet test src/Modules/Weather/Tests/Weather.Modules.Weather.Tests --filter Category=Temperature`
2. Add `RelativeHumidity` + Magnus `DewPoint.Derive` (clamped to `min(derived, temperature)`) → verify: `dotnet test src/Modules/Weather/Tests/Weather.Modules.Weather.Tests --filter Category=DewPoint`
3. Add `WindInfo` + `CardinalDirection` → verify: `dotnet test src/Modules/Weather/Tests/Weather.Modules.Weather.Tests --filter Category=WindInfo`
4. Add `SkyCondition`, `Visibility`, `Pressure`, `Location` → verify: `dotnet test src/Modules/Weather/Tests/Weather.Modules.Weather.Tests --filter Category=WeatherValueObjects`
5. Add `WeatherReport.Create` + `WeatherDomainException` → verify: `dotnet test src/Modules/Weather/Tests/Weather.Modules.Weather.Tests --filter Category=WeatherReport`

## 13. Test plan
- SC-e03s01-P0-01: 32°F→0°C, 212°F→100°C, 98.6°F→37°C, −40°F→−40°C, negative Celsius.
- SC-e03s01-P0-02: dew point ≤ temperature; at 100% RH dew point ≈ temperature (±0.1).
- Cardinal boundaries (0/90/180/270/360); humidity/watchdog guards throw `WeatherDomainException`.
- `WeatherReport.Create` rejects a non-UTC timestamp; computes dew point from temperature + humidity.

## 14. Acceptance criteria (Gherkin)
```gherkin
Scenario Outline: Fahrenheit to Celsius
  Given a Fahrenheit temperature of <f>
  When I convert it
  Then Celsius is <c>
  Examples:
    | f    | c   |
    | 32   | 0   |
    | 212  | 100 |
    | -40  | -40 |
    | 98.6 | 37  |

Scenario: Dew point never exceeds temperature
  Given air at 20C and 100% relative humidity
  When I derive the dew point
  Then the dew point is <= 20C
  And it equals 20C

Scenario: Missing wind direction is variable
  Given a wind with speed but no direction
  When I build the WindInfo
  Then DirectionDegrees is null
  And Cardinal is "Variable"

Scenario: Non-UTC timestamp rejected
  Given a report with a local timestamp
  Then WeatherDomainException is thrown
```

## 15. Definition of done
All domain tests green; Domain references BCL only; no BuildingBlocks types used.

## 16. Verification script
1. `dotnet test src/Modules/Weather/Tests/Weather.Modules.Weather.Tests`
2. `dotnet build src/Modules/Weather/Weather.Modules.Weather.Domain/Weather.Modules.Weather.Domain.csproj --warnaserror`

## 17. Out of scope
Kelvin; unit-selection UI; history.

## 18. Risks / notes
`WeatherReport` is a record, not an `AggregateRoot` — deliberate (no persistence).

## 19. Sizing / BCP rationale
5 BCPs — several value objects with real math and invariants.

## 20. References
- `specs/adr/0003-weather-provider-and-dew-point.md`
- `specs/adr/0007-weather-module-read-only-self-contained.md`
- `specs/product/GLOSSARY_LATEST.yaml`
