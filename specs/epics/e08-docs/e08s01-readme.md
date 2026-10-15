# Story e08s01 — GeoWeather README (new-joiner bootstrap) + AI instructions

| Field | Value |
|-------|-------|
| ID | e08s01 |
| Epic | e08 Documentation & Repository Tooling |
| Status | done |
| BCPs | 3 |
| Risk | P3 |
| Type | docs |
| Delta | ADDED |

## 1. Summary
Write a `README.md` that bootstraps a new joiner on **GeoWeather** in one read: what it is, how to run it,
how it is built, and where the deep docs live. The README is a **hybrid**: core sections are inline; deep
topics link to the authoritative spec. Broaden this story from its original skeleton to the 15-section
structure below and verify every documented command by running it.

## 2. Context
Requirement S8 mandates a README plus a root AI instructions file; `AGENTS.md`/`CONVENTIONS.md` already exist.
This story was originally scoped to a modular-monolith overview only (Maturity 3). It is broadened to the
agreed 15-section structure so a joiner needs no other hand-off.

## 3. User story
As a new joiner, I want a single README that tells me what GeoWeather is, how to run and test it, and where
each deeper document lives, so I am productive without tribal knowledge.

## 4. Business value
Documentation is a graded deliverable and the primary onboarding surface.

## 5. In scope
- `README.md` with 15 sections (see §12), a table of contents, a logo header, and consistent bullet icons.
- **Hybrid depth:** inline Overview, Key Features, Technology Stack, Quick Start, Architecture, Repository
  Structure, Configuration, Security (summary), Testing (summary), Troubleshooting, Known Limitations, Contributing;
  link out Development Guide, Observability, Deployment to their authoritative docs.
- Record the four agreed decisions (§21).
- Verify every documented command actually runs (§13).
- Log the discovered spec drift (Favorites/Notes vs Journal) as a defect instead of copying stale tables.

## 6. Out of scope
- `## License` (decision Q1 — omitted).
- CI/CD or pipeline changes; containers; a hosted docs site.
- Duplicating the full content of `docs/` and `specs/` (Q4 — hybrid, link out).
- Correcting the stale `TEST_PLAN_LATEST.md` / `tech-stack.md` module lists (logged as BUG-003).

## 7. Domain concepts
None. The README *describes* the domain (Geography, Weather, Journal) but introduces no new concepts.

## 8. Layer touchpoints
| Assembly | Change |
|----------|--------|
| docs | new `README.md` |
| docs | confirm `AGENTS.md`, `CONVENTIONS.md` still match the built solution |
| specs | `epics/e08-docs/e08s01-readme.md`, `e08s01-tasks.yaml`, `epic.yaml`; `bugs/registry.yaml` (BUG-003) |

## 9. API contract
Documents the existing endpoints; no endpoint changes.

| Method | Route |
|--------|-------|
| GET | `/api/countries?search=` |
| GET | `/api/countries/{countryCode}/cities` |
| GET | `/api/weather/{cityName}?countryCode=` |
| GET | `/api/weather/{cityName}/forecast?countryCode=` |
| GET | `/api/cities/favorites` |
| POST | `/api/cities/favorites` |
| POST | `/api/weather/notes` |
| GET | `/health/live`, `/health/ready`, `/version` |

## 10. Dependencies
All prior epics. No code dependency.

## 11. Risks
| Risk | Mitigation |
|------|-----------|
| Docs drift (specs already list merged-away Favorites/Notes modules) | Source README facts from code; log drift as BUG-003; keep deep detail linked, not copied |
| Documented command does not run | Task 6 runs build/format/test and curls the live endpoints |
| Invented badges/links | No badge row; only links to files that exist in the repo |

## 12. Implementation steps
Each step follows `N. <do> → verify: <command>`.

1. Broaden this story spec to the 15-section hybrid structure and record decisions Q1–Q4 →
   verify: `grep -q 'GeoWeather' specs/epics/e08-docs/e08s01-readme.md && grep -q 'Q1' specs/epics/e08-docs/e08s01-readme.md`
2. Write README front matter: H1 + logo, TOC, Overview, Key Features, Technology Stack, Repository Structure →
   verify: `grep -q 'geoweather-mark.svg' README.md && grep -q 'MediatR' README.md && grep -q 'BuildingBlocks' README.md`
3. Write Quick Start + Configuration (verified commands, ports 5121/7137, config keys) →
   verify: `grep -q 'dotnet run' README.md && grep -q 'OpenWeather__ApiKey' README.md`
4. Write Architecture (module table, IntegrationEvents, per-module DbContext, ADR links) →
   verify: `grep -q 'IntegrationEvents' README.md && grep -q 'DbContext' README.md`
5. Write the remaining sections: Development Guide, Security, Observability, Testing, Deployment,
   Troubleshooting, Known Limitations, Contributing →
   verify: `grep -q 'CONVENTIONS.md' README.md && grep -q 'parity:styles' README.md && grep -qi 'limitation' README.md`
6. Run every documented command and confirm no API key is committed and no `## License` section exists →
   verify: `dotnet build Weather.sln --warnaserror && dotnet format Weather.sln --verify-no-changes && dotnet test Weather.sln`
7. Log the spec drift as BUG-003 →
   verify: `grep -q 'BUG-003' specs/bugs/registry.yaml`

## 13. Test plan
Documentation claims are verified by executing the documented commands: `dotnet restore`, `dotnet build
--warnaserror`, `dotnet format --verify-no-changes`, `dotnet test`, `dotnet run`, and `curl` against
`/health/live`, `/health/ready`, `/version`. No automated test is added; the artifact is prose plus a defect log.

## 14. Acceptance criteria (Gherkin)
```gherkin
Scenario: A new joiner can run the project from the README
  Given a fresh clone with .NET 10 installed
  When I follow the README Quick Start
  Then the host starts and /health/ready returns ready
  And the documented Preflight command exits 0

Scenario: The architecture is explained without leaving the README
  Then the README names the three bounded contexts and their data isolation
  And it states that a module references another only via IntegrationEvents
  And it links to the ADRs

Scenario: Deep detail is not duplicated
  Then the Observability, Deployment and Development Guide sections link to their source docs
  And the README does not restate the full module test-project matrix

Scenario: No License section
  Then the README contains no "## License" heading
  And the table of contents has no License entry
```

## 15. Definition of done
`README.md` complete with 15 sections; all documented commands executed and green; no API key committed; no
License section; BUG-003 logged; AI files (`AGENTS.md`, `CONVENTIONS.md`) still accurate.

## 16. Verification script
1. Fresh clone (or clean tree); run the Quick Start block verbatim.
2. Confirm `/health/live`, `/health/ready`, `/version` respond.
3. Run the Preflight command; confirm exit 0.
4. Open the README on a renderer; confirm every anchor in the TOC resolves and every linked path exists.

## 17. Out of scope
Hosted documentation site; auto-generated API reference.

## 18. Risks / notes
Live weather needs `OpenWeather:ApiKey`; tests are offline. Development starts without a key and degrades weather
calls to `502 DEPENDENCY_UNAVAILABLE`.

## 19. Sizing / BCP rationale
3 BCPs — one document plus a verification pass and one defect log.

## 20. References
- `AGENTS.md`, `CONVENTIONS.md`
- `specs/adr/0005-modular-monolith-and-modules.md`, `0012`, `0016`
- `specs/tech-architecture/tech-stack.md`, `TEST_PLAN_LATEST.md`, `SECURITY_PLAN_LATEST.md`,
  `OBSERVABILITY_PLAN_LATEST.md`, `AZURE_PIPELINE_PLAN_LATEST.md`
- `docs/operations/observability.md`, `docs/engineering/csharp-standards.md`, `docs/engineering/rest-api-standards.md`

## 21. Decisions (locked)

| # | Decision | Value |
|---|----------|-------|
| Q1 | License | **Omit** the `## License` section and its TOC entry (no license is declared in the repo) |
| Q2 | Title | H1 is **GeoWeather** (product name; logo `src/Weather.WebUI/wwwroot/img/geoweather-mark.svg`). Solution/assemblies remain `Weather.*`. |
| Q3 | Contributing | Document **both**: the mandatory Preflight gate, then "PR if collaborating, else merge to `main` only when Preflight is green" |
| Q4 | Depth | **Hybrid** — inline the core; link out Development Guide, Observability, Deployment to their authoritative docs |
