# Story e03s01 — Weather domain: WeatherReport, Temperature conversion, dew point

| Field | Value |
|-------|-------|
| ID | e03s01 |
| Epic | e03 Weather Module (M2, core) |
| Status | todo |
| BCPs | 5 |
| Risk | P0 |
| Type | feat |

## 1. Summary
Build the Weather module's framework-free domain: `Temperature` (F↔C), `DewPoint` (Magnus), `WindInfo`,
`SkyCondition`, `Visibility`, `Pressure`, `RelativeHumidity`, `Location`, and the `WeatherReport` aggregate.

## 2. Context
This is the core domain of the application and the one piece of real business logic. Fahrenheit arrives from
the provider and is converted here.

## 3. User story
As a developer, I want a tested weather domain model, so metrics are correct and provider-independent.

## 4. Business value
Correctness of every metric shown to users.

## 5. In scope
- `Temperature`: `FromFahrenheit`, `FromCelsius`, `Celsius`, `Fahrenheit`, equality, 1-decimal rounding.
- `RelativeHumidity` + `DewPoint.Derive(Temperature, RelativeHumidity)` (Magnus).
- `WindInfo` (m/s, degrees, cardinal), `SkyCondition`, `Visibility`, `Pressure`, `Location`.
- `WeatherReport` aggregate with invariants.
- Domain unit tests.

## 6. Out of scope
Provider mapping (e03s02); query/endpoint (e03s03).

## 7. Domain concepts
`WeatherReport`, `Temperature`, `DewPoint`, `RelativeHumidity`, `WindInfo`, `SkyCondition`, `Visibility`, `Pressure`, `Location`.

## 8. Layer touchpoints
| Assembly | Change |
|----------|--------|
| Weather.Domain | all value objects + aggregate |

## 9. API contract
None (pure domain).

## 10. Dependencies
e01s01 (BuildingBlocks.Domain).

## 11. Risks
| Risk | Mitigation |
|------|-----------|
| Floating-point drift | Single rounding rule + tests |
| -40 equilibrium | Explicit P0 test |

## 12. Implementation steps
1. Add `Temperature` VO with conversions → verify: `dotnet test src/Modules/Weather/Tests --filter Category=Temperature`
2. Add `RelativeHumidity` + Magnus `DewPoint.Derive` → verify: `dotnet test src/Modules/Weather/Tests --filter Category=DewPoint`
3. Add `WindInfo` + cardinal derivation → verify: `dotnet test src/Modules/Weather/Tests --filter Category=Wind`
4. Add `SkyCondition`, `Visibility`, `Pressure`, `Location`, `WeatherReport` → verify: `dotnet test src/Modules/Weather/Tests --filter Category=WeatherReport`
5. Add business rules and guard clauses → verify: `dotnet test src/Modules/Weather/Tests --filter Category=Invariants`

## 13. Test plan
SC-e03s01-P0-01/02. 32→0, 212→100, 98.6→37, -40→-40, negatives; dew point ≤ temperature; cardinal boundaries; invalid humidity throws.

## 14. Acceptance criteria (Gherkin)
```gherkin
Scenario Outline: Fahrenheit to Celsius
  Given <f> Fahrenheit
  When converted
  Then Celsius is <c>
  Examples:
    | f    | c   |
    | 32   | 0   |
    | 212  | 100 |
    | -40  | -40 |
    | 98.6 | 37  |

Scenario: Dew point bounded by temperature
  Given 20C and 100% humidity
  Then dew point == 20C within 0.1
```

## 15. Definition of done
Domain tests green; no framework references.

## 16. Verification script
1. `dotnet test src/Modules/Weather/Tests`.
2. `dotnet test src/Tests/ArchTests --filter Category=DomainLayer`.

## 17. Out of scope
Kelvin; unit preference UI.

## 18. Risks / notes
Dew point is derived (ADR-0003).

## 19. Sizing / BCP rationale
5 BCPs — several VOs with real math and invariants.

## 20. References
- `specs/adr/0003-weather-provider-and-dew-point.md`
- `specs/product/GLOSSARY_LATEST.yaml`
