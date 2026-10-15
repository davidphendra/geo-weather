# Azure DevOps Pipeline Plan — Weather

> Status: decisions locked (Q1–Q7). Pipeline file: `azure-pipelines.yml` at the repo root.
> CI/CD runs on Azure DevOps; the previous GitHub Actions workflow has been removed.

## Decisions

| # | Question | Decision |
|---|----------|----------|
| Q1 | Test scope | **All offline tests** — `dotnet test Weather.sln` (unit + architecture + in-process integration). The Playwright browser e2e harness is **not** run. |
| Q2 | Deploy target | **Azure App Service (Linux)**, framework-dependent zip deploy. |
| Q3 | Deploy gate | **None — automatic deploy.** The `Deploy` job is a plain `job` with no `environment:` and no approval check. |
| Q4 | Shape | **One pipeline**: CI on PRs + `main`; a `Deploy` stage that runs only on `main`, after CI, with no approval gate. |
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
  dotnet format --verify-no-changes          download webapp artifact
  Tailwind CSS drift guard                   AzureWebApp@1  zipDeploy → App Service (Linux)
  dotnet test (no e2e) + TRX + coverage
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
   connection with Contributor on the App Service.
2. **Variable group** — create a Library variable group named **`weather-cicd`** and authorize it for
   this pipeline (Library → group → Pipeline permissions). Without the authorization the run fails
   with *variable group not authorized*. Members:

   | Key | Purpose |
   |-----|---------|
   | `azureServiceConnection` | name of the ARM service connection from step 1 |
   | `webAppName` | App Service name |
   | `dotnetRuntimeStack` | e.g. `DOTNETCORE\|10.0` |
   | `vmImage` | agent image for the Deploy job, e.g. `ubuntu-latest` |
   | `appType` | e.g. `webAppLinux` |
   | `deploymentMethod` | e.g. `zipDeploy` |

   The group is referenced **only by the `Deploy` stage**, so the `CI` stage runs on pull requests
   even when the group is not linked. Secrets (e.g. `OpenWeather__ApiKey`) do **not** belong in this
   group; they live in App Service configuration / Key Vault (step 5).
3. **Values that stay in YAML** — repo-structural constants (`buildConfiguration`, `solution`,
   `webProject`, `testResultsDirectory`) and toolchain defaults (`dotnetSdkVersion`, `nodeVersion`,
   `vmImage`) are declared in `azure-pipelines.yml`. The Deploy stage's group-scoped `vmImage`
   overrides the root default (narrowest scope wins).
4. **No Environment/approval needed** — the `Deploy` job is a plain `job` with no `environment:`
   and no approval check, so CD runs automatically. Do not attach the deploy to an Environment
   that carries an Approval check.
5. **App Service configuration** (App Service → Configuration):
   - `Database__DataDirectory` = `/home/data` (persistent on App Service Linux). The host creates the
     directory and the three SQLite files at startup (ADR-0013).
   - `OpenWeather__ApiKey` = your key, ideally as a **Key Vault reference** (never in the repo).
   - `ASPNETCORE_ENVIRONMENT` = `Production` (CSP is enforced outside Development).
6. **Branch policy** — add a build policy on `main` requiring the `CI` stage, so PRs must be green.

### Parameter provenance

| Value | Where it lives | Why |
|-------|----------------|-----|
| `azureServiceConnection`, `webAppName`, `dotnetRuntimeStack`, `vmImage`, `appType`, `deploymentMethod` | `weather-cicd` variable group | environment-scoped; change without a commit |
| `buildConfiguration`, `solution`, `webProject`, `testResultsDirectory`, `dotnetSdkVersion`, `nodeVersion` | `azure-pipelines.yml` | repo-structural / toolchain defaults |
| `trigger`/`pr` branch lists, deploy `condition` | `azure-pipelines.yml` (literal) | evaluated at compile time; variables are not expanded there |

## Why the deploy is automatic

The `Deploy` stage runs without human approval. It `dependsOn: CI`, it only runs when
`Build.SourceBranch == refs/heads/main`, and it is a plain `job` with **no `environment:` and no
approval check**, so a green CI on `main` deploys straight to App Service. The gate was removed
from the YAML (rather than by deleting the Environment's Approval check) so the automatic-deploy
decision is codified in git; the trade-off is the loss of Environment deployment history.

## Verification

- `python3 -c "import yaml; yaml.safe_load(open('azure-pipelines.yml'))"` — valid YAML (run locally).
- Azure DevOps itself validates the schema when the pipeline is created; the YAML editor shows errors
  before the first run.
- The Tailwind drift guard is independently verifiable locally: `bash scripts/check-tailwind-css.sh`.

## Versioning integration (e16 — additive, not yet applied)

When e16 lands, the pipeline gains artifact-level semantic versioning. The changes are specified in
`specs/tech-architecture/VERSIONING_PLAN_LATEST.md` §4.4 and `specs/adr/0019-application-and-artifact-versioning.md`;
this plan remains the pipeline authority. Summary of the touch points:

| # | Change | Reason |
|---|--------|--------|
| P1 | `checkout: self` gains `fetchDepth: 0` | NBGV needs full git history; a shallow clone yields `0.0.0` |
| P2 | NBGV sets `Build.BuildNumber` to the version (`##vso[build.updatebuildnumber]`) | one label for run + artifact + app |
| P3 | `scripts/check-version.sh` runs after publish | fail loudly instead of shipping `0.0.0` |
| P4 | CI artifact renamed to `webapp-$(Build.BuildNumber)` | artifact-level identity |
| P5 | Deploy `download` artifact name matches P4 | must move together |
| P6 | Deploy `AzureWebApp@1` `package` path matches P4 | must move together |
| P7 | optional build tag `$(GitBuildVersionSimple)` | filter run history by version |
| P8 | optional `v*` tag trigger + Deploy `condition` | build/deploy tagged releases (D9) |

Note: name the artifact with `Build.BuildNumber` (predefined, visible in the Deploy stage), **not** NBGV's
`GitBuildVersionSimple` (`task.setvariable` is job-scoped and invisible to Deploy). PR builds are unaffected
beyond a non-public version suffix; they never deploy.

## Notes / risks

- **Unused `production` Environment** — if one was created with an Approval check, the pipeline no
  longer references it. It can be deleted (Pipelines → Environments) or left in place; it no longer
  affects CD.
- **`wire-ci` (bigpowers)** ships templates for GitHub only; Azure DevOps is unsupported, so this
  pipeline was written by hand (the documented fallback for unsupported forges).
- **.NET 10 on App Service Linux** must be offered as `DOTNETCORE|10.0`. If the stack is not yet
  listed, change `dotnetRuntimeStack` or switch the publish to `--self-contained` with
  `RuntimeIdentifier linux-x64`.
- **SQLite on App Service** requires the `/home` persistent path; the default `App_Data` is ephemeral
  across restarts otherwise.
- **Playwright/e2e** is intentionally out of CI; add a scheduled pipeline later if you want it.
