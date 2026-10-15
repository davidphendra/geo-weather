# ADR-0019 — Application and artifact versioning (SemVer)

**Status:** Proposed (awaiting decision confirmation — see `decisions_to_confirm` in
`specs/tech-architecture/VERSIONING_PLAN_LATEST.md`)
**Date:** 2026-10-15
**Relates to:** ADR-0011 (Problem Details `traceId`), ADR-0012 (single host), ADR-0013 (App_Data),
ADR-0014 (OpenTelemetry → Azure Monitor), `specs/tech-architecture/AZURE_PIPELINE_PLAN_LATEST.md`

## Context

The application ships as a zip artifact to Azure App Service (Linux) via Azure DevOps. Today it has **no
semantic version**:

- No `version.json`, no `<Version>`, and no git tags exist (`release.last_tag: null` in `specs/state.yaml`).
- Every assembly therefore defaults to `AssemblyVersion 1.0.0.0`.
- `AppHeader.razor` displays `Assembly.GetName().Version` — the *assembly identity* version — which is
  `1.0.0` on every build regardless of the source revision.
- The Azure DevOps run number is not tied to a version, and the published `webapp` artifact carries no
  version or commit identity.
- The OpenTelemetry resource (ADR-0014) sets `deployment.environment` and `cloud.provider` but **not**
  `service.version`, so Application Insights cannot filter traces/errors by app version.

The operations team needs to answer: *"Which build is causing this incident, and what commit is it?"*
without guessing and without redeploying. That requires a version that is **baked into the artifact at
build time** and **visible at runtime**.

### Requirements

- One canonical semantic version per build, derived from source (not injected at deploy).
- The deployed artifact, the CI run, the runtime, and telemetry all agree on that version.
- The version is visible to a user without tooling (footer) and to ops/probes (endpoint, log, telemetry).
- Every version maps to a source commit for root-cause work.
- No live network call in tests; no secret; no framework dependency in the Domain layer.
- Deterministic and repeatable: the same commit produces the same version on the same public-release ref.

## Decision

Adopt **Semantic Versioning 2.0.0** as the product version and **Nerdbank.GitVersioning (NBGV)** as the
single source of truth, deriving the version from git height plus a committed `version.json`, and surface
it at every layer from build to runtime.

| # | Decision | Proposed value |
|---|----------|----------------|
| D1 | Version source | **Nerdbank.GitVersioning 3.10.x** with a repo-root `version.json` |
| D2 | Baseline / scheme | `version.json` `"version": "0.1"` → `0.1.<height>` (pre-1.0, per SemVer §4) |
| D3 | Assembly stamping | `AssemblyVersion` = `major.minor` (stable identity); `FileVersion` = full + height; `InformationalVersion` = full SemVer + `+g<sha>` |
| D4 | Runtime surfaces | Footer + `GET /version` + startup log + OTel `service.version` |
| D5 | Displayed value | `InformationalVersion` (never `Assembly.GetName().Version`) |
| D6 | Artifact identity | ADO run number = version (NBGV cloud build); `build-info.json` inside the publish output; artifact name `webapp-$(Build.BuildNumber)` |
| D7 | SourceLink | Deferred — no git remote exists yet; the commit id is already in `InformationalVersion` |
| D8 | Failed/no-git fallback | Report `0.0.0-unknown`; a build guard and a pipeline guard fail the run if the version is `0.0.0` |
| D9 | Release tags | Add a `v*` tag trigger and extend the Deploy condition so tagged commits deploy |
| D10 | Build-info metric | Host-local `GeoWeather.Host` meter; `geoweather.build.info` ObservableGauge (value 1) with `version`/`commit` attributes |

### Specifics

1. **Version source (D1).** A `version.json` at the repository root is committed with the base version.
   NBGV computes the final version from the git *height* (commits since the version changed) and the
   commit id. It works with or without a remote, needs a full clone in CI, and stamps all assemblies.

2. **Assembly stamping (D3).** NBGV injects, for each assembly:
   `[AssemblyVersion("0.1")]`, `[AssemblyFileVersion("0.1.24.15136")]`,
   `[AssemblyInformationalVersion("0.1.24-alpha+g9a7eb6c819")]`.
   Because the app loads its own assemblies, `AssemblyVersion` stays at `major.minor` so patch builds do
   not create binding churn; the free-form `InformationalVersion` is the diagnostic identity.

3. **Runtime reader (D4/D5).** A single `AppVersion` type in `Weather.WebUI` reads the entry assembly's
   `AssemblyInformationalVersionAttribute` once and exposes `SemanticVersion`, `Commit`,
   `InformationalVersion`. The footer and header consume it; nothing calls `GetName().Version` for display.

4. **Operational surface (D4).** `GET /version` returns
   `{ "version": "0.1.24-alpha", "informationalVersion": "0.1.24-alpha+g9a7eb6c819", "commit": "9a7eb6c819" }`
   and is un-authenticated like `/health/*`; it is safe to cache. `Program.cs` prints **one console line at
   startup, before `app.Run()`** — `GeoWeather {Version} ({Commit}) starting in {Environment}` — so the
   version is visible in platform logs (`docker logs`, App Service log stream) without a request. No secret,
   path, or environment config is leaked.

5. **Telemetry correlation (D4/D10).** The OpenTelemetry resource gains `service.name = "GeoWeather"` and
   `service.version = <informational version>`, alongside the existing `deployment.environment`. Because a
   resource attribute is attached to every signal, this alone makes logs, traces, metrics, and the
   Application Map filterable by version. In addition, the host emits an explicit
   **`geoweather.build.info`** ObservableGauge (value `1`, attributes `version` and `commit`) from a
   host-local `GeoWeather.Host` meter. It answers "which versions are live and how many instances of
   each", supports rollout/canary monitoring and version-drift alerts, and does not depend on resource
   attributes being surfaced by every backend. It is host-local (like runtime instrumentation) because
   BuildingBlocks must not know the application version.

6. **Artifact identity (D6).** `cloudBuild.buildNumber.enabled: true` sets the Azure DevOps run number to the
   computed version (`##vso[build.updatebuildnumber]`), so the build history, the artifact, and the deployed
   app share one label. A `build-info.json` (version, commit, build id, built-at) is emitted into the publish
   output so the artifact is self-describing without running it. The pipeline artifact is published as
   `webapp-$(Build.BuildNumber)` and the Deploy stage downloads the same name. `Build.BuildNumber` (a
   predefined, cross-stage variable) is used deliberately instead of NBGV's job-scoped
   `GitBuildVersionSimple`, which is not visible in the Deploy stage. **Azure Pipelines must
   `checkout: self` with `fetchDepth: 0`** — a shallow clone erases the history NBGV needs and yields
   `0.0.0`; a build-time guard and a post-publish guard make that failure loud. Deploy never injects or
   rewrites the version.

7. **CI prerequisites (D1).** Azure Pipelines must `checkout: self` with `fetchDepth: 0`; NBGV documents
   that shallow clones erase the history required for accurate version calculation. The build fails (D8)
   if a version cannot be derived.

## Evidence — real-world sources

| Source | Statement used |
|--------|----------------|
| [SemVer 2.0.0](https://semver.org/spec/v2.0.0.html) | `MAJOR.MINOR.PATCH`; pre-release `-alpha.1`, build metadata `+sha`; a released version is immutable; `0.y.z` is initial development. |
| [Microsoft: Assembly versioning](https://learn.microsoft.com/dotnet/standard/assembly/versioning) | Two distinct versions: the identity version (runtime binding) and the *informational* version (diagnostics only, "not used at runtime"). |
| [Microsoft: Library guidance — versioning](https://learn.microsoft.com/dotnet/standard/library-guidance/versioning) | `FileVersion` uses `Major.Minor.Build.Revision`; consider a CI build number in the revision; "**AVOID setting the assembly informational version yourself** — allow SourceLink to generate the version containing NuGet and source-control metadata." |
| [NBGV `version.json`](https://dotnet.github.io/Nerdbank.GitVersioning/docs/versionJson.html) | `version`, `assemblyVersion.precision`, `publicReleaseRefSpec`, `cloudBuild.buildNumber`, `gitCommitIdIncludeDirty`. A third integer moves git height to the 4th component; version height increases monotonically. |
| [NBGV cloud build support](https://dotnet.github.io/Nerdbank.GitVersioning/docs/cloudbuild.html) | "`cloudBuild.buildNumber.enabled` … set your cloud build's build number to equal the semver version"; "`setVersionVariables`" exposes `GitBuildVersion`/`GitAssemblyInformationalVersion` to later steps; Azure Pipelines requires `checkout: self; fetchDepth: 0`. |
| [NBGV .NET stamping](https://dotnet.github.io/Nerdbank.GitVersioning/docs/ecosystems/dotnet.html) | Injects `AssemblyVersion`, `AssemblyFileVersion`, `AssemblyInformationalVersion` and a `ThisAssembly` class with `AssemblyInformationalVersion`. |
| [OpenTelemetry service semantic conventions](https://opentelemetry.io/docs/specs/semconv/resource/service/) | `service.version` is **Stable/Recommended**: "The version string of the service component"; `service.name` is Required and must be identical for horizontally scaled instances. |
| [Azure Monitor OpenTelemetry configuration](https://learn.microsoft.com/azure/azure-monitor/app/opentelemetry-configuration) | Resource attributes (`service.name`, `service.namespace`, `service.instance.id`) drive the Application Map and filtering. |
| [Azure Pipelines run numbers](https://learn.microsoft.com/azure/devops/pipelines/process/run-number) | `name:` / `Build.BuildNumber` controls the run label; `$(Rev:r)` is monotonic per day; NBGV overrides it with the SemVer. |

> Verified at planning time (2026-10-15) against the live NuGet feed
> (`Nerdbank.GitVersioning` stable **3.10.94**) and the sources above.

## Alternatives rejected

- **MinVer 8.x** (git-tag driven, MIT, very small). Rejected: the repo has no tags and tracks an intended
  version in `release-plan.yaml`; MinVer has no `version.json` SSOT and no cloud-build-number integration.
- **GitVersion** (branch-model driven). Rejected: high configuration cost and a branch model (GitFlow) the
  repo does not use; overkill for one deployable.
- **Manual `<Version>` in `Directory.Build.props` + pipeline `-p:Version=`**. Rejected: no local-dev
  version, drift-prone, and violates build-once/deploy-anywhere.
- **Inject the version at deploy time** via an App Service app setting. Rejected: the artifact must be
  immutable and self-describing; deploy-time injection breaks rollback forensics and reproducibility.
- **Keep showing `Assembly.GetName().Version`.** Rejected: it is the binding identity, not the build
  identity, and never changes between patch builds.
- **Hand-maintained `CHANGELOG`/version string.** Rejected: guarantees drift; git is the SSOT.

## Consequences

**Positive**
- Ops can filter logs, traces, and a dashboard by `service.version` and map it to a commit.
- A support ticket can quote the footer/`/version` value; a probe can scrape it.
- The CI run, artifact, and runtime all carry the same SemVer; CI history becomes a release ledger.
- The version is reproducible from a commit — no hand-managed numbers.

**Negative / costs**
- Adds one build-time NuGet package (NBGV) to central package management; it is `PrivateAssets="all"` and
  ships no runtime dependency.
- CI must use a full clone (`fetchDepth: 0`), slightly slower checkout.
- The unit-test projects and `dotnet format --verify-no-changes` must be validated against NBGV's generated
  `obj/` sources (expected clean; verified in the first task).
- Pre-1.0 (`0.y.z`) means the "public API" contract is not yet stable, which matches the product state.

**Neutral**
- SourceLink is deferred until a git remote exists; the commit id is already embedded in the informational
  version, so traceability is intact now.
