# Azure DevOps Pipeline Plan — Weather

> Status: decisions locked (Q1–Q7). Pipeline file: `azure-pipelines.yml` at the repo root.
> CI/CD runs on Azure DevOps; the previous GitHub Actions workflow has been removed.

## Decisions

| # | Question | Decision |
|---|----------|----------|
| Q1 | Test scope | **All offline tests** — `dotnet test Weather.sln` (unit + architecture + in-process integration). The Playwright browser e2e harness is **not** run. |
| Q2 | Deploy target | **Azure App Service (Linux)**, framework-dependent zip deploy. |
| Q3 | Deploy gate | **Azure DevOps Environment approval** on `environment: production` (no automatic deploy). |
| Q4 | Shape | **One pipeline**: CI on PRs + `main`; a `Deploy` stage that runs only on `main`, after CI, and waits for the Environment approval. |
| Q5 | Extra gates | `dotnet format --verify-no-changes` **and** the Tailwind CSS drift guard (`scripts/check-tailwind-css.sh`). |
| Q6 | Reporting | Publish **test results (TRX)** and **code coverage** (visibility only, no threshold). |
| Q7 | GitHub Actions | **Removed** — Azure is the single CI/CD system; the drift-guard script stays and is called by the pipeline. |

## Pipeline shape

```
trigger: pull_request + main ─────────────┐
                                           ▼
stage CI  (ubuntu-latest)                  stage Deploy  (ubuntu-latest)
  restore                                    dependsOn: CI
  build --warnaserror                        condition: main only
  dotnet format --verify-no-changes          environment: production  ◄── approval gate
  Tailwind CSS drift guard                     download webapp artifact
  dotnet test (no e2e) + TRX + coverage        AzureWebApp@1  zipDeploy → App Service (Linux)
  dotnet publish → artifact "webapp"
```

## What CI runs (and does not)

- **Runs:** the 8 test projects in `Weather.sln` — module unit suites, `Weather.BuildingBlocks.Tests`,
  `Weather.WebUI.UnitTests`, `Weather.ArchTests`, `Weather.IntegrationTests` (in-process
  `WebApplicationFactory`, no network).
- **Does not run:** `npm run parity:styles` (Playwright, browser e2e). It needs a running app and a
  Chromium download, which belongs on a developer machine or a dedicated nightly job, not PR validation.

## One-time setup

1. **Service connection** — Project settings → Service connections → new **Azure Resource Manager**
   connection with Contributor on the App Service. Set the pipeline variable
   `azureServiceConnection` to its name.
2. **Pipeline variables** — set `webAppName` (and `dotnetRuntimeStack` if it differs) in the pipeline
   UI, or in a Library variable group. `dotnetRuntimeStack` defaults to `DOTNETCORE|10.0`.
3. **Environment + approval** — Pipelines → Environments → create `production` → **Approvals and
   checks** → add an **Approval** (add yourself/your team). This is what makes CD wait for a user.
4. **App Service configuration** (App Service → Configuration):
   - `Database__DataDirectory` = `/home/data` (persistent on App Service Linux). The host creates the
     directory and the three SQLite files at startup (ADR-0013).
   - `OpenWeather__ApiKey` = your key, ideally as a **Key Vault reference** (never in the repo).
   - `ASPNETCORE_ENVIRONMENT` = `Production` (CSP is enforced outside Development).
5. **Branch policy** — add a build policy on `main` requiring the `CI` stage, so PRs must be green.

## Why the deploy is not automatic

The `Deploy` stage is gated three ways: it `dependsOn: CI`, it only runs when
`Build.SourceBranch == refs/heads/main`, and it targets the `production` Environment whose
**Approval** check suspends the run until a user approves. No approval ⇒ no deployment.

## Verification

- `python3 -c "import yaml; yaml.safe_load(open('azure-pipelines.yml'))"` — valid YAML (run locally).
- Azure DevOps itself validates the schema when the pipeline is created; the YAML editor shows errors
  before the first run.
- The Tailwind drift guard is independently verifiable locally: `bash scripts/check-tailwind-css.sh`.

## Notes / risks

- **`wire-ci` (bigpowers)** ships templates for GitHub only; Azure DevOps is unsupported, so this
  pipeline was written by hand (the documented fallback for unsupported forges).
- **.NET 10 on App Service Linux** must be offered as `DOTNETCORE|10.0`. If the stack is not yet
  listed, change `dotnetRuntimeStack` or switch the publish to `--self-contained` with
  `RuntimeIdentifier linux-x64`.
- **SQLite on App Service** requires the `/home` persistent path; the default `App_Data` is ephemeral
  across restarts otherwise.
- **Playwright/e2e** is intentionally out of CI; add a scheduled pipeline later if you want it.
