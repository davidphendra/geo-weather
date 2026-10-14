# Impact — Merge `Weather.Api` into `Weather.WebUI`

## Target
`src/Weather.Api` (12 files) and every artifact that references it. Proposed: one ASP.NET Core host
(`Weather.WebUI`) serving both the Blazor UI and the REST API.

## Dependents (direct, non-generated)
- `src/Tests/Weather.IntegrationTests/Weather.IntegrationTests.csproj` — references `Weather.Api`; host is
  `WebApplicationFactory<Program>` in `Support/WeatherHostFactory.cs`.
- `src/Tests/Weather.ArchTests/Weather.ArchTests.csproj` — references `Weather.Api` (ApiTests area isolation).
- `src/Tests/Weather.Api.UnitTests/Weather.Api.UnitTests.csproj` — references `Weather.Api`; 5 controller test
  files use `using Weather.Api.Modules.*`.
- `src/Weather.Api/Modules/*/*Controller.cs` (4) + `Common/ApiControllerBase.cs` + `ErrorHandling/*` (3) +
  `Program.cs` + `Properties/launchSettings.json` + `appsettings*.json`.
- `Weather.sln` — project entries + `src` nesting.
- Docs/specs: `AGENTS.md`, `CONVENTIONS.md`, `specs/tech-architecture/{tech-stack,TEST_PLAN_LATEST}.md`,
  `specs/data/...` (none), ADR-0005, ADR-0009, `specs/epics/e00-repo-layout/e00s01-repo-layout.md`,
  `specs/epics/e06-webui/e06s01-dropdowns.md` + tasks, `specs/epics/e07-quality-gates/e07s01-*`,
  `specs/PLAN_LATEST.md`, `specs/release-plan.yaml`, `specs/execution-status.yaml`, `specs/state.yaml`.

## Affected Stories
- **e00s01** (done) — reference graph `Weather.WebUI → (none)` and `Weather.Api` host node become invalid.
- **e06s01/e06s02** (todo) — `WeatherApiClient` base address, CORS task (same-origin ⇒ unnecessary), "no server
  project reference" note.
- **e07s01** (todo) — `ApiTests` scope moves from `Weather.Api` to the WebUI's API area.
- **e09** (done) — REST contract must survive byte-identically; only the hosting assembly changes.

## Test Coverage
- `Weather.IntegrationTests` — full HTTP surface via `WebApplicationFactory<Program>` (endpoints, errors,
  ProblemDetails, validation, OpenAPI). These are the regression net for the merge.
- `Weather.Api.UnitTests` — controller result-type tests with stubbed `I<Module>Service`.
- `Weather.ArchTests.ModuleIsolationTests` — module boundaries (unaffected by the host move).
- **Gap:** nothing asserts that the *Blazor host still serves the API* after the merge, that API `404` is not
  rewritten by `UseStatusCodePagesWithReExecute`, or that `UseHttpsRedirection` does not break the test host.
  New/updated tests required (see plan).

## Runtime-wiring hazards (must be reconciled in one pipeline)
- `UseExceptionHandler()` (API ProblemDetails) vs `UseExceptionHandler("/Error", …)` (Blazor) — collide.
- `UseStatusCodePagesWithReExecute("/not-found")` must not swallow API 404 ProblemDetails.
- `UseHttpsRedirection` (Blazor-only today) must not redirect the integration test host.
- `UseAntiforgery` (Blazor) coexisting with API POST controllers.
- `MapStaticAssets` + `MapControllers` + `MapRazorComponents<App>` endpoint ordering.

## Risk: High
Shared composition root + HTTP pipeline + 3 test projects + 6 spec/doc surfaces; the contract must be
byte-identical. No direct callers of a public API library, but the host is the integration point for everything.

## Recommended action
Confirm the interpretation/naming decisions (see `specs/REFACTOR_LATEST.md` decision list) before drafting tasks.
