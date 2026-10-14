# Story e07s02 — Offline end-to-end harness + coverage gate

| Field | Value |
|-------|-------|
| ID | e07s02 |
| Epic | e07 Architecture & Offline Test Gates |
| Status | todo |
| BCPs | 3 |
| Risk | P0 |
| Type | feat |

## 1. Summary
Finalize the shared end-to-end harness: boot the host with a stub `IWeatherService`, per-module SQLite `:memory:`
contexts, and an inline Events Bus; add an offline guard test and wire coverage.

## 2. Context
The hard constraint is "all tests run offline with zero live network calls". This story makes it explicit.

## 3. User story
As a developer, I want a reliable offline harness, so the suite is deterministic.

## 4. Business value
Turns the executive constraint into a verifiable property.

## 5. In scope
- `WeatherHostFactory` replacing `IWeatherService` and each module's DbContext with test doubles.
- Per-module `:memory:` connections (one open connection each).
- Inline outbox publisher so integration events flow deterministically in tests.
- Offline guard test + coverage collection.

## 6. Out of scope
Mutation/load testing.

## 7. Domain concepts
None (harness).

## 8. Layer touchpoints
| Assembly | Change |
|----------|--------|
| src/Tests/IntegrationTests | factory + fixtures + guard + coverage |

## 9. API contract
None.

## 10. Dependencies
e01–e05.

## 11. Risks
| Risk | Mitigation |
|------|-----------|
| `:memory:` connection closed between requests | One open connection per module context |
| Accidental network | Guard test + stub handler + non-routable base URL in test config |

## 12. Implementation steps
1. Extract `WeatherHostFactory` with overridden DI → verify: `dotnet test src/Tests/IntegrationTests`
2. Add per-module `:memory:` contexts + inline outbox publisher → verify: `dotnet test src/Tests/IntegrationTests --filter Category=Stubs`
3. Add offline guard test (real provider never resolved) → verify: `dotnet test src/Tests/IntegrationTests --filter Category=OfflineGuard`
4. Add coverage collection + report → verify: `dotnet test Weather.sln --collect:\"XPlat Code Coverage\"`
5. Confirm the full suite passes offline → verify: `dotnet test Weather.sln`

## 13. Test plan
- Guard: real `OpenWeatherMapService` not resolved.
- All end-to-end tests share the harness; suite < 90s.

## 14. Acceptance criteria (Gherkin)
```gherkin
Scenario: No live provider in tests
  Given the test host
  When the weather service is resolved
  Then it is the stub, not OpenWeatherMapService

Scenario: Suite is deterministic
  Given no network access
  When I run dotnet test Weather.sln
  Then all tests pass
```

## 15. Definition of done
Full suite green offline; guard present; coverage reported.

## 16. Verification script
1. `dotnet test Weather.sln` with Wi-Fi off → all green.

## 17. Out of scope
Contract tests against the real provider.

## 18. Risks / notes
Coverage threshold is report-only.

## 19. Sizing / BCP rationale
3 BCPs — harness + guard + coverage.

## 20. References
- `specs/tech-architecture/TEST_PLAN_LATEST.md`
