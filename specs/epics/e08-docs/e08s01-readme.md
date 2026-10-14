# Story e08s01 — README + AI instructions + GitHub-ready repo

| Field | Value |
|-------|-------|
| ID | e08s01 |
| Epic | e08 Documentation & Repository Tooling |
| Status | todo |
| BCPs | 3 |
| Risk | P3 |
| Type | docs |

## 1. Summary
Write the README (modular-monolith architecture, bounded contexts, module slicing, setup/build/run/test, EF Core
per-module contexts, design decisions, AI prompt file), finalize the AI instructions, and confirm the repo is
GitHub-ready.

## 2. Context
Requirement 6 mandates a README and a root AI instructions file; `AGENTS.md`/`CONVENTIONS.md` already exist.

## 3. User story
As an evaluator, I want clear docs, so I can understand and run the project.

## 4. Business value
Documentation is a graded deliverable.

## 5. In scope
- README: modular-monolith overview (bounded contexts, module = assembly set, IntegrationEvents, Events Bus),
  module map table, prerequisites, setup/build/run/test, EF Core per-module config, design decisions, AI file.
- Confirm `AGENTS.md` (+ `CLAUDE.md`) + `CONVENTIONS.md` match the built solution.
- `.gitignore` review; no secrets tracked.

## 6. Out of scope
CI/CD pipelines; containers.

## 7. Domain concepts
None.

## 8. Layer touchpoints
| Assembly | Change |
|----------|--------|
| docs | `README.md`, confirm `AGENTS.md`, `CONVENTIONS.md` |

## 9. API contract
Documents the existing endpoints.

## 10. Dependencies
All prior epics.

## 11. Risks
| Risk | Mitigation |
|------|-----------|
| Docs drift | Write last; run every documented command |

## 12. Implementation steps
1. Draft the modular-monolith overview → verify: `grep -qi 'modular monolith' README.md && grep -q 'Bounded context' README.md`
2. Add setup/build/run/test instructions → verify: `grep -q 'dotnet test' README.md && grep -q 'dotnet run' README.md`
3. Document the module map + IntegrationEvents → verify: `grep -q 'Geography' README.md && grep -q 'IntegrationEvents' README.md`
4. Document EF Core per-module contexts + design decisions → verify: `grep -q 'DbContext' README.md && grep -qi 'design decisions' README.md`
5. Document the AI instructions file → verify: `grep -q 'CONVENTIONS.md' README.md && grep -q 'AGENTS.md' README.md`
6. Confirm clean build and no secrets → verify: `dotnet build Weather.sln --warnaserror`

## 13. Test plan
Doc claims verified by running the documented commands.

## 14. Acceptance criteria (Gherkin)
```gherkin
Scenario: Evaluator can run the project
  Given a fresh clone
  When I follow README setup/build/run/test
  Then the API starts and tests pass

Scenario: Architecture is explained
  Then the README explains bounded contexts, module slicing, and integration events
  And it documents the AI instructions file
```

## 15. Definition of done
README complete; commands verified; AI files accurate; clean build.

## 16. Verification script
1. Follow README in a temp clone; confirm build/run/test.

## 17. Out of scope
Hosted docs site.

## 18. Risks / notes
State that live weather needs an API key and that tests are offline.

## 19. Sizing / BCP rationale
3 BCPs — documentation + verification pass.

## 20. References
- `AGENTS.md`, `CONVENTIONS.md`
- `specs/adr/0005-modular-monolith-and-modules.md`
