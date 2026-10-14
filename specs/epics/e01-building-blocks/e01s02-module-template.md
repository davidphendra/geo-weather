# Story e01s02 — Module template + InMemory Events Bus + Outbox + cross-module arch tests

| Field | Value |
|-------|-------|
| ID | e01s02 |
| Epic | e01 BuildingBlocks & Modular Host |
| Status | todo |
| BCPs | 5 |
| Risk | P0 |
| Type | feat |

## 1. Summary
Establish the module template (an empty Geography slice as the reference module), the InMemory Events Bus,
the per-module Outbox with a background publisher, module initialization from the host, and the cross-module
architecture tests that forbid any dependency other than `IntegrationEvents`.

## 2. Context
This is the load-bearing story of the modular monolith: it makes the reference's rules executable before feature
code exists.

## 3. User story
As a developer, I want the module skeleton and event plumbing, so modules can be built without coupling.

## 4. Business value
Prevents architectural drift; every later module plugs into the same template.

## 5. In scope
- `Weather.Modules.Geography.{Domain,Application,Infrastructure,IntegrationEvents}` skeleton + `IGeographyModule` + `GeographyModule` + `GeographyContext`.
- `BuildingBlocks.Infrastructure`: `IInMemoryEventsBus`, `InMemoryEventsBus`, `Outbox`, background publisher, `UnitOfWork`.
- Host initializes modules (`AddGeographyModule()` + `Initialize`), `EventsBusStartup`.
- `src/Tests/ArchTests/Modules/ModuleTests.cs` — per-module "no dependency on other modules except IntegrationEvents".
- `src/Tests/ArchTests/Api/ApiTests.cs` — API area isolation.
- `specs/tech-architecture/modules.map` committed.

## 6. Out of scope
Geography features (e02); other modules (e03–e05).

## 7. Domain concepts
None (infrastructure plumbing).

## 8. Layer touchpoints
| Assembly | Change |
|----------|--------|
| BuildingBlocks.Infrastructure | EventsBus, Outbox, publisher |
| Weather.Modules.Geography.* | skeleton |
| Weather.Api | module init + Geography area |
| src/Tests/ArchTests | ModuleTests + ApiTests |

## 9. API contract
`GET /api/health/modules` → `200 { "modules": ["Geography"] }` (proves per-module init).

## 10. Dependencies
e01s01.

## 11. Risks
| Risk | Mitigation |
|------|-----------|
| Background publisher races with startup | Deliver after all modules initialized; tests run the publisher inline. |
| Cross-module test passes vacuously | Require a seeded type per module before the rule is considered green. |

## 12. Implementation steps
1. Create the Geography module skeleton (4 assemblies) + `IGeographyModule`/`GeographyModule`/`GeographyContext` → verify: `dotnet build Weather.sln --warnaserror`
2. Add `IInMemoryEventsBus`/`InMemoryEventsBus` to BuildingBlocks.Infrastructure → verify: `dotnet test src/Tests/ArchTests --filter Category=EventsBus`
3. Add per-module `Outbox` + domain-event dispatcher + background publisher → verify: `dotnet test src/Modules/Geography/Tests --filter Category=Outbox`
4. Host initializes the module and exposes `GET /api/health/modules` → verify: `dotnet test src/Tests/IntegrationTests --filter Category=ModuleInit`
5. `ModuleTests`: a module must not depend on another module except IntegrationEvents → verify: `dotnet test src/Tests/ArchTests --filter Category=ModuleIsolation`
6. `ApiTests`: API areas are module-isolated → verify: `dotnet test src/Tests/ArchTests --filter Category=ApiIsolation`
7. Commit `specs/tech-architecture/modules.map` → verify: `test -f specs/tech-architecture/modules.map`

## 13. Test plan
- Module init test: host boots and reports the module.
- EventsBus test: a published event reaches a subscriber.
- Outbox test: a domain event is persisted then published.
- Arch tests: forbidden dependency fails the build.

## 14. Acceptance criteria (Gherkin)
```gherkin
Scenario: Module is initialized by the host
  When the host starts
  Then GET /api/health/modules returns ["Geography"]

Scenario: Event reaches a subscriber
  Given a subscriber registered on the InMemory Events Bus
  When an integration event is published
  Then the subscriber receives it

Scenario: Module cannot depend on another module
  Given the Geography Domain/Application/Infrastructure assemblies
  When a type references another module's namespace
  Then the ModuleTests arch test fails
```

## 15. Definition of done
Module skeleton builds; EventsBus + Outbox tests green; cross-module arch test green.

## 16. Verification script
1. `dotnet run --project src/Weather.Api` → `curl localhost:5000/api/health/modules`.
2. `dotnet test src/Tests/ArchTests` green.

## 17. Out of scope
Business features.

## 18. Risks / notes
Deviates from the reference's Autofac per-module container (ADR-0005) — Microsoft DI instead.

## 19. Sizing / BCP rationale
5 BCPs — event plumbing + module template + arch rules.

## 20. References
- `specs/adr/0005-modular-monolith-and-modules.md`
- `specs/tech-architecture/modules.map`
