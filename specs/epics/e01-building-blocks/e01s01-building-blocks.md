# Story e01s01 — BuildingBlocks + thin API host + walking skeleton

| Field | Value |
|-------|-------|
| ID | e01s01 |
| Epic | e01 BuildingBlocks & Modular Host |
| Status | todo |
| BCPs | 5 |
| Risk | P0 |
| Type | feat |

## 1. Summary
Create the three `BuildingBlocks` assemblies (Domain/Application/Infrastructure) and the thin `Weather.Api` host,
then prove the host works end-to-end with a `GET /api/ping` endpoint.

## 2. Context
The reference ships `src/BuildingBlocks/{Domain,Application,Infrastructure}` consumed by every module. This story
lays those foundations before any bounded context exists.

## 3. User story
As a developer, I want shared DDD/infrastructure primitives and a host, so modules can be added uniformly.

## 4. Business value
Every later module depends on it; establishes the build and composition baseline.

## 5. In scope
- `Weather.BuildingBlocks.Domain`: `Entity`, `AggregateRoot`, `ValueObject`, `TypedIdValueBase`, `DomainEventBase`, `IDomainEvent`, `IBusinessRule`, `BusinessRuleValidationException`, `IAggregateRoot`.
- `Weather.BuildingBlocks.Application`: `ICommand<T>`, `IQuery<T>`, `CommandBase`, `QueryBase`, validation abstractions, EventsBus abstractions.
- `Weather.BuildingBlocks.Infrastructure`: EF base, `UnitOfWork`, `Clock`, domain-event dispatcher placeholder.
- `Weather.Api` host with `Program.cs`, ProblemDetails, OpenAPI, `GET /api/ping`.
- Move `Weather.WebUI` under `src/`.
- `AGENTS.md`/`CONVENTIONS.md` present.

## 6. Out of scope
Any bounded context; EventsBus/Outbox (e01s02).

## 7. Domain concepts
`Entity`, `AggregateRoot`, `ValueObject` (BuildingBlocks only).

## 8. Layer touchpoints
| Assembly | Change |
|----------|--------|
| BuildingBlocks.Domain | new |
| BuildingBlocks.Application | new |
| BuildingBlocks.Infrastructure | new |
| Weather.Api | new host |
| Weather.WebUI | moved under src/ |

## 9. API contract
`GET /api/ping` → `200 { "status": "ok", "service": "Weather.Api" }`

## 10. Dependencies
None (first epic).

## 11. Risks
| Risk | Mitigation |
|------|-----------|
| BuildingBlocks becomes a dumping ground (stop condition) | Size guard: only the listed primitives; a later test fails on growth. |
| Moving WebUI breaks the sln | Re-add all projects; build gate. |

## 12. Implementation steps
1. Create `Weather.BuildingBlocks.Domain` with the DDD primitives → verify: `dotnet build Weather.sln --warnaserror`
2. Create `Weather.BuildingBlocks.Application` (ICommand/IQuery/CommandBase/QueryBase) → verify: `dotnet build Weather.sln --warnaserror`
3. Create `Weather.BuildingBlocks.Infrastructure` (EF base, UnitOfWork, Clock) → verify: `dotnet build Weather.sln --warnaserror`
4. Create `Weather.Api` host + `GET /api/ping` + ProblemDetails + OpenAPI → verify: `dotnet test src/Tests/IntegrationTests --filter Category=Ping`
5. Move `Weather.WebUI` under `src/` and repair the sln → verify: `dotnet build Weather.sln --warnaserror`
6. Domain-slice test: `BuildingBlocks.Domain` references only `System.*` → verify: `dotnet test src/Tests/ArchTests --filter Category=BuildingBlocksBoundary`

## 13. Test plan
- Integration: ping returns 200; unknown route → 404 ProblemDetails.
- Architecture: BuildingBlocks.Domain has no framework references.

## 14. Acceptance criteria (Gherkin)
```gherkin
Scenario: Host responds
  Given the API host is running
  When I GET /api/ping
  Then the status is 200 and the body contains "status": "ok"

Scenario: BuildingBlocks.Domain stays pure
  Given the compiled BuildingBlocks.Domain assembly
  Then it references only System.*
```

## 15. Definition of done
Solution builds clean; ping test green.

## 16. Verification script
1. `dotnet run --project src/Weather.Api` → `curl localhost:5000/api/ping`.
2. `dotnet test Weather.sln` green.

## 17. Out of scope
Modules, events, persistence.

## 18. Risks / notes
Match the reference's BuildingBlocks split (Domain/Application/Infrastructure).

## 19. Sizing / BCP rationale
5 BCPs — three shared assemblies + host + first slice.

## 20. References
- `specs/adr/0005-modular-monolith-and-modules.md`
- `specs/tech-architecture/tech-stack.md`
