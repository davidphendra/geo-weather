# Story e10s01 — Merge `Weather.Api` into `Weather.WebUI` (single host)

| Field | Value |
|-------|-------|
| ID | e10s01 |
| Epic | e10 Single Host |
| Status | todo |
| BCPs | 6 |
| Risk | P0 |
| Type | refactor |
| ADR | ADR-0012 |

## 1. Summary
Delete the `Weather.Api` project and fold its content into `Weather.WebUI`, producing one ASP.NET Core host that
serves both the Blazor UI and the REST API. The REST surface, its e09 response contract, and all module
boundaries must be unchanged.

## 2. Context
ADR-0005 introduced a thin `Weather.Api` host plus a Blazor `Weather.WebUI` "client". The WebUI was never wired
to the API and there are no external API consumers; two processes are pure overhead. ADR-0012 collapses them.

## 3. User story
As a maintainer, I want one web project, so that there is a single deployable, a single port, and no
self-referential CORS/config to keep in sync.

## 4. Business value
Less deployment/configuration surface with no loss of function; the REST contract stays available.

## 5. In scope
- Move `Common/`, `ErrorHandling/`, and `Modules/*Controller.cs` into `Weather.WebUI/Api/` (namespace
  `Weather.WebUI.Api.*`).
- Merge the API service registration and controller/OpenAPI mapping into `Weather.WebUI/Program.cs`, plus
  `public partial class Program;`.
- Path-scope exception handling and status-code pages so API responses stay Problem Details and Blazor keeps
  its `/Error` and `/not-found` pages.
- Add module references + `Microsoft.AspNetCore.OpenApi` to `Weather.WebUI.csproj`.
- Delete `src/Weather.Api`; repoint `Weather.IntegrationTests`, `Weather.ArchTests`, and
  `Weather.WebUI.UnitTests` references; update `Weather.sln`.
- Rename `Weather.Api.UnitTests` → `Weather.WebUI.UnitTests`.
- Regression tests for the single host (`Category=SingleHost`).
- Spec/doc updates: ADR-0012, AGENTS.md, CONVENTIONS.md, tech-stack.md, TEST_PLAN, capsules, release plan.

## 6. Out of scope
- Changing any route, status code, or response body.
- Changing module internals, data isolation, or the Events Bus.
- Rewiring Blazor to call modules in-process (ADR-0012 alternative B).
- The e06 client work itself (country/city dropdowns, weather render).

## 7. Domain concepts
None (host/composition-root change).

## 8. Layer touchpoints
| Assembly | Change |
|----------|--------|
| `Weather.Api` | deleted |
| `Weather.WebUI` | gains `Api/**`, module references, merged `Program` |
| `Weather.IntegrationTests` | reference → WebUI |
| `Weather.ArchTests` | reference → WebUI |
| `Weather.WebUI.UnitTests` | renamed, reference → WebUI, usings updated |

## 9. API contract
Unchanged: `/api/countries`, `/api/countries/{countryCode}/cities`, `/api/weather/{cityName}`,
`POST /api/weather/notes`, `/api/cities/favorites` (GET/POST) — same status codes and RFC 9457 bodies.

## 10. Dependencies
None. Independent of open features; should land before e06 so the client targets the final host.

## 11. Risks
| Risk | Mitigation |
|------|-----------|
| Blazor `UseExceptionHandler("/Error")` collides with API Problem Details | Path-scope the API handler to `/api` |
| `UseStatusCodePagesWithReExecute` rewrites API 404s | Scope it off `/api` |
| `UseHttpsRedirection` (new to the API surface) redirects the test host | Verify via integration tests; harden factory if needed |
| Ambiguous global `Program` during transition | `public partial class Program` exists only in WebUI after the atomic move |
| Deleting a project breaks references | Tasks 3–4 land as one atomic commit |

## 12. Implementation steps
1. Add module refs + OpenApi package to WebUI → verify: `dotnet build src/Weather.WebUI/Weather.WebUI.csproj --warnaserror`
2. Move API source into `Weather.WebUI/Api/`, rename namespaces → verify: `dotnet build src/Weather.WebUI/Weather.WebUI.csproj --warnaserror`
3. Merge the API pipeline into `Program.cs` (path-scoped error/status handling) → verify: `dotnet build src/Weather.WebUI/Weather.WebUI.csproj --warnaserror`
4. Delete `Weather.Api`, repoint all references, update sln → verify: `dotnet build Weather.sln --warnaserror && dotnet test Weather.sln`
5. Rename `Weather.Api.UnitTests` → `Weather.WebUI.UnitTests` → verify: `dotnet test src/Tests/Weather.WebUI.UnitTests`
6. Reconcile launch settings/config → verify: `dotnet build Weather.sln --warnaserror`
7. Add `Category=SingleHost` regression tests → verify: `dotnet test Weather.sln --filter Category=SingleHost`
8. Verify HTTPS redirection does not affect the test host → verify: `dotnet test src/Tests/Weather.IntegrationTests --filter Category=ProblemDetails`
9. Add ADR-0012 + cross-refs → verify: `test -f specs/adr/0012-merge-api-into-webui.md`
10. Update AGENTS.md + CONVENTIONS.md → verify: `grep -q "src/Weather.WebUI" AGENTS.md`
11. Update tech-stack.md + TEST_PLAN + PLAN_LATEST → verify: `! grep -rn "Weather\.Api" specs/tech-architecture/`
12. Update capsules + release-plan + execution-status + state → verify: `! grep -rn "Weather\.Api" specs/epics/`
13. Full preflight → verify: `dotnet build Weather.sln --warnaserror && dotnet format Weather.sln --verify-no-changes && dotnet test Weather.sln`

## 13. Test plan
- Existing `Weather.IntegrationTests` (27) remain the contract regression net.
- New `SingleHostTests` (`Category=SingleHost`): `/api/countries` returns JSON; `/` returns HTML; an unknown
  `/api/*` route does not return HTML; error responses carry `code` + `traceId`.
- Existing `Weather.Api.UnitTests` → `Weather.WebUI.UnitTests` (7) unchanged in behaviour.

## 14. Acceptance criteria (Gherkin)
```gherkin
Scenario: The API is served by the WebUI host
  When I GET /api/countries from the running app
  Then the response is 200 with application/json

Scenario: The UI is served by the same host
  When I GET / 
  Then the response is 200 with text/html

Scenario: API errors stay Problem Details
  When I GET /api/weather/Nowhere
  Then the response is 404 application/problem+json with code RESOURCE_NOT_FOUND and a traceId

Scenario: One project remains
  Then Weather.sln contains no Weather.Api project
  And it contains Weather.WebUI and Weather.WebUI.UnitTests
```

## 15. Definition of done
Build (`--warnaserror`), format (`--verify-no-changes`), and `dotnet test Weather.sln` all green; `Weather.Api`
gone; all references repointed; specs/docs consistent.

## 16. Verification script
1. `dotnet build Weather.sln --warnaserror`
2. `dotnet format Weather.sln --verify-no-changes`
3. `dotnet test Weather.sln`
4. `dotnet run --project src/Weather.WebUI` → `curl /api/countries`, `curl /`

## 17. Out of scope
Section 6.

## 18. Risks / notes
Tasks 3–4 are atomic (a deleted project invalidates all references). Task 5 (rename) is optional but recommended.

## 19. Sizing / BCP rationale
6 BCPs — file moves + Program merge + reference repointing + test project rename + new host tests + spec sweep.

## 20. References
- `specs/adr/0012-merge-api-into-webui.md`
- `specs/IMPACT_LATEST.md`
