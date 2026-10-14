# Story e07s01 — Per-module + cross-module architecture test suite

| Field | Value |
|-------|-------|
| ID | e07s01 |
| Epic | e07 Architecture & Offline Test Gates |
| Status | todo |
| BCPs | 5 |
| Risk | P0 |
| Type | feat |

## 1. Summary
Complete the architecture suite that makes the modular monolith's rules executable: per-module layer rules,
cross-module isolation (only IntegrationEvents crosses), API-area isolation, and domain immutability.

## 2. Context
The reference enforces these in `src/Tests/ArchTests` and per-module `Tests/ArchTests`. This story ports the rules.

## 3. User story
As a developer, I want boundary violations to fail the build, so the architecture cannot rot.

## 4. Business value
Makes the modular-monolith claim provable.

## 5. In scope
- Cross-module `ModuleTests` (no dependency on other modules except IntegrationEvents) for all four modules.
- API `ApiTests` (each API area isolated).
- Per-module `LayersTests` (Domain ↛ Application/Infrastructure; Application ↛ Infrastructure).
- Per-module `DomainTests` (value objects + domain events immutable; non-root entities no public members; entities do not reference other aggregate roots).
- BuildingBlocks.Domain purity + size guard.

## 6. Out of scope
Behaviour tests.

## 7. Domain concepts
None.

## 8. Layer touchpoints
| Assembly | Change |
|----------|--------|
| src/Tests/ArchTests | ModuleTests, ApiTests, BuildingBlocks |
| src/Modules/<M>/Tests/ArchTests | LayersTests, DomainTests per module |

## 9. API contract
None.

## 10. Dependencies
e01s02 (first rules), e02–e05 (real module types).

## 11. Risks
| Risk | Mitigation |
|------|-----------|
| False positives on generated types | Scope to project namespaces; ignore obj/bin/Migrations |
| Vacuous rules | Require at least one seeded type per module |

## 12. Implementation steps
1. Cross-module `ModuleTests` for all four modules → verify: `dotnet test src/Tests/ArchTests --filter Category=ModuleIsolation`
2. API `ApiTests` → verify: `dotnet test src/Tests/ArchTests --filter Category=ApiIsolation`
3. Per-module `LayersTests` → verify: `dotnet test src/Modules/Geography/Tests --filter Category=Layers` (repeat per module)
4. Per-module `DomainTests` (immutability + encapsulation) → verify: `dotnet test src/Modules/Weather/Tests --filter Category=DomainRules`
5. BuildingBlocks.Domain purity + size guard → verify: `dotnet test src/Tests/ArchTests --filter Category=BuildingBlocksBoundary`

## 13. Test plan
Each rule references real types; a temporary violation proves the rule bites, then removed.

## 14. Acceptance criteria (Gherkin)
```gherkin
Scenario: No module depends on another except IntegrationEvents
  Given the four modules' Domain/Application/Infrastructure assemblies
  Then no type depends on another module's namespace
  Except INotificationHandler<> / *IntegrationEventHandler / Module startup types

Scenario: Layers hold inside a module
  Then Domain has no dependency on Application or Infrastructure
  And Application has no dependency on Infrastructure

Scenario: API area isolation
  Given the API assembly
  Then each API area has no dependency on another module's namespace
```

## 15. Definition of done
Full suite green; every rule demonstrably bites.

## 16. Verification script
1. `dotnet test src/Tests/ArchTests`.
2. `dotnet test Weather.sln --filter Category=Arch`.

## 17. Out of scope
Runtime behaviour.

## 18. Risks / notes
Mirrors the reference's `ModuleTests.cs` / `LayersTests.cs` / `DomainTests.cs`.

## 19. Sizing / BCP rationale
5 BCPs — four rule families across four modules.

## 20. References
- `specs/adr/0005-modular-monolith-and-modules.md`
- `specs/tech-architecture/TEST_PLAN_LATEST.md`
