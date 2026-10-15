# BUG-2026-10-15: DesignTokenTests cannot find app.css in a clean build output

## Problem

**Actual behaviour.** `dotnet test` fails in the Azure DevOps `CI — build, format, tests` job
with:

```
System.IO.FileNotFoundException : Could not find file
'/home/vsts/work/1/s/src/Tests/Weather.WebUI.UnitTests/bin/Release/net10.0/app.css'.
```

The exception is wrapped in a `System.TypeInitializationException` for
`Weather.WebUI.UnitTests.Design.DesignTokenTests`; all 15 tests in the class fail before a single
assertion runs.

**Expected behaviour.** The design-token guard reads the committed compiled stylesheet from its own
build output and asserts the Telemetry Slate tokens are present. It must pass on a clean CI clone
with no Node/Tailwind step, exactly as its design intends ("config and compiled CSS are copied into
the test output, so the check needs no Node").

**Reproduction (deterministic, local).**

```bash
rm -rf src/Tests/Weather.WebUI.UnitTests/bin/Release src/Tests/Weather.WebUI.UnitTests/obj/Release
dotnet build src/Tests/Weather.WebUI.UnitTests/Weather.WebUI.UnitTests.csproj -c Release
dotnet test  src/Tests/Weather.WebUI.UnitTests/Weather.WebUI.UnitTests.csproj -c Release --no-build \
  --filter "Category=DesignTokens"
```

Result: `Failed: 15, Passed: 0` with the identical `FileNotFoundException` for the local
`bin/Release/net10.0/app.css` path.

**Security impact: NONE.** A missing build artifact; no data flow, auth, or secret exposure path.

## Root Cause Analysis

The failing test reads two files from `AppContext.BaseDirectory` in a static field initializer:

- `tailwind.config.js` — present in the test output.
- `app.css` — absent.

The test project delivers both files as build-output content items. `tailwind.config.js` is still
declared; the `app.css` content item was **deleted** by commit `2f6c3db` ("Remove and regenerate
app.css"). That commit's message describes regenerating the stylesheet, but its only diff removes
the `app.css` `<Content … CopyToOutputDirectory="PreserveNewest" />` item from the test project.
The file itself stayed committed under `src/Weather.WebUI/wwwroot/app.css` (confirmed with
`git ls-files`). So the deletion of the copy rule was almost certainly an accident bundled into a
stylesheet commit.

Why it looked green locally and failed in CI:

- `wwwroot/app.css` is a static web asset of the referenced `Weather.WebUI` project. A prior full
  build had already deposited a copy in the test project's `bin/Debug` output, so an incremental
  local run found a leftover file and passed.
- CI builds `Release` from a clean clone. Nothing copies `wwwroot/app.css` into the test output, so
  the static initializer throws on the first touch of `DesignTokenTests`.

Contributing factor: the pipeline's `dotnet test` runs with `--no-build`, so it cannot paper over
the missing content item by re-copying; and the "Tailwind CSS drift guard" step was removed in
`89b3973`, so nothing else verifies the compiled stylesheet in CI.

Risk level: **Low.** Build/packaging regression, fully deterministic, single line to fix; no
production runtime impact.

## TDD Fix Plan

1. **RED**: Add a regression test that fails on a clean output directory when `app.css` is not
   delivered to the test output — i.e. the `DesignTokenTests` compiled-CSS check must run against
   the file at `AppContext.BaseDirectory`. (Today the class-level static initializer is itself the
   failure; the tightest RED is to build the test project into a fresh output directory and observe
   the `FileNotFoundException`.)
   **GREEN**: Restore the `app.css` content item in the test project so the committed stylesheet is
   copied to the output:
   `<Content Include="..\..\Weather.WebUI\wwwroot\app.css" Link="app.css" CopyToOutputDirectory="PreserveNewest" />`
   **verify**: `rm -rf src/Tests/Weather.WebUI.UnitTests/bin/Release src/Tests/Weather.WebUI.UnitTests/obj/Release && dotnet build src/Tests/Weather.WebUI.UnitTests/Weather.WebUI.UnitTests.csproj -c Release && dotnet test src/Tests/Weather.WebUI.UnitTests/Weather.WebUI.UnitTests.csproj -c Release --no-build --filter "Category=DesignTokens"`

2. **RED**: Guard against the class reappearing: a test that asserts the copy contract (the stylesheet
   the design tests read is present in the test output) so a future edit to the project file cannot
   silently regress it.
   **GREEN**: Keep the assertion focused on behaviour — "the compiled stylesheet is available to the
   test assembly" — rather than on project-file text.
   **verify**: `dotnet test src/Tests/Weather.WebUI.UnitTests/Weather.WebUI.UnitTests.csproj -c Release`

3. **RED** (optional hardening): Restore a CI drift check so the committed `app.css` is proven to
   match `tailwind.config.js` + `Styles/tailwind.css` (`scripts/check-tailwind-css.sh` exists; it was
   dropped from `azure-pipelines.yml` in `89b3973`).
   **GREEN**: Re-add the guard step after `dotnet format` in the CI stage, or document the deliberate
   removal in ADR/comment so the omission is intentional.
   **verify**: `bash scripts/check-tailwind-css.sh` (requires Node; CI has it via `NodeTool@0`).

**REFACTOR**: none expected; the fix is the restored content item. Optionally add the CI drift guard
note to the pipeline header comment.

## Acceptance Criteria

- [ ] A clean `Release` build of `Weather.WebUI.UnitTests` places `app.css` in the test output.
- [ ] `dotnet test … --filter "Category=DesignTokens"` passes 15/15 on a clean output directory.
- [ ] Full Preflight is green: `dotnet build Weather.sln --warnaserror && dotnet format Weather.sln --verify-no-changes && dotnet test Weather.sln`.
- [ ] The Azure DevOps `dotnet test` step no longer reports `FileNotFoundException` for `app.css`.
- [ ] No new tests are weakened and existing tests still pass.

## Resolution

Fixed on branch `fix/design-tokens-css-copy`.

- Restored the `app.css` content item in `Weather.WebUI.UnitTests.csproj` (`Link="app.css"`,
  `CopyToOutputDirectory="PreserveNewest"`).
- Reworked `DesignTokenTests` to read its inputs lazily and added a
  `Design_sources_are_copied_to_the_test_output` theory that asserts both `tailwind.config.js` and
  `app.css` reach the test output, turning a missing artifact into a clear, actionable failure
  instead of a `TypeInitializationException`.
- Corrected the stale `azure-pipelines.yml` header comment: the Tailwind drift guard is author-time
  only and is not run in CI. Re-adding it is deferred pending a decision (it was removed in
  `89b3973` while the stylesheet was being regenerated).

**Evidence.**

- RED reproduced on a clean output: `Failed: 15, Passed: 0` with the identical
  `FileNotFoundException` for `bin/Release/net10.0/app.css`.
- Guard RED (temporarily dropping the content item):
  `app.css must be copied to the test output; see the Content items in Weather.WebUI.UnitTests.csproj.`
  (1 failed, 1 passed — only the absent file fails).
- GREEN on a clean Release build: `Passed: 17, Failed: 0` (`Category=DesignTokens`).
- Preflight: `dotnet build Weather.sln --warnaserror` → 0 warnings/0 errors;
  `dotnet format Weather.sln --verify-no-changes` → clean;
  `dotnet test Weather.sln` → 420 passed, 0 failed.

**Files changed**

- `src/Tests/Weather.WebUI.UnitTests/Weather.WebUI.UnitTests.csproj`
- `src/Tests/Weather.WebUI.UnitTests/Design/DesignTokenTests.cs`
- `azure-pipelines.yml` (comment accuracy only)
- `specs/bugs/registry.yaml`, `specs/bugs/BUG-004-*.md`
