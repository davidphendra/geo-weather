# Versioning Plan — Application & Artifact SemVer

> Status: **PLANNED — decisions D1–D8 pending confirmation** (see §10).
> Decision record: `specs/adr/0019-application-and-artifact-versioning.md`.
> Epic capsule: `specs/epics/e16-app-versioning/`.
> This file is the versioning authority for e16.

## 1. Goal and operating scenarios

Give an operations team a single, trustworthy answer to **"which build is this, and what commit is it?"**
at every point in the delivery chain.

| Audience | Question | Surface |
|----------|----------|---------|
| End user / support | "What version do you see at the bottom of the page?" | Footer |
| Platform log / `docker logs` | "What version just started?" | Console startup line |
| Operator / probe | "What version is this deployment running?" | `GET /version` (and startup log) |
| SRE / on-call | "Show me all failing requests for version 0.1.24" | OTel `service.version` in Application Insights |
| SRE / on-call | "Which versions are live, and how many instances of each?" | OTel `geoweather.build.info` metric grouped by `version` |
| Engineer | "Which commit produced this artifact?" | `InformationalVersion` `+g<sha>` / `build-info.json` |
| Release owner | "Which artifact did we deploy?" | ADO run number + `webapp-<version>` artifact |

### Non-goals

- No semantic-release automation (CHANGELOG generation, commit-message parsing).
- No container/OCI labels (zip deploy, not containers).
- No SourceLink/remote pinning until a git remote exists.
- No change to module boundaries, Domain, or the REST error contract.

## 2. Research synthesis (real-world practice)

**Semantic Versioning 2.0.0** — `MAJOR.MINOR.PATCH`; pre-release `-alpha.1`, build metadata `+sha`; a
released version is immutable; `0.y.z` is initial development. The version must communicate intent about
compatibility.

**Three distinct ".NET versions"** (a classic trap):

| Attribute | Purpose | Correct content | Used for display? |
|-----------|---------|-----------------|-------------------|
| `AssemblyVersion` | Assembly **identity** / binding | `major.minor` (stable) | **No** |
| `AssemblyFileVersion` | Windows file metadata | `major.minor.patch.<build>` | No |
| `AssemblyInformationalVersion` | **Diagnostic identity** (free-form) | `major.minor.patch[-pre]+g<sha>` | **Yes** |

Microsoft guidance: *"AVOID setting the assembly informational version yourself — allow SourceLink to
generate the version containing NuGet and source-control metadata."* The app's current header reads
`Assembly.GetName().Version`, which is exactly the wrong attribute — it never moves between patch builds.

**Git-derived versioning tools** (all require a full clone):

| Tool | SSOT | Strength | Fit here |
|------|------|----------|----------|
| **Nerdbank.GitVersioning** | `version.json` | deterministic, version *height*, cloud-build run number, `ThisAssembly` | **Chosen** |
| MinVer | git tags | smallest possible | no tags exist; no `version.json`; no run-number integration |
| GitVersion | branch model | rich branch/CI output | overkill; no GitFlow here |

**Artifact identity is a chain, not one field.** The convention that holds up in production is: the CI run
number **is** the version, the artifact **name** carries it, the artifact **contains** a build manifest, the
assemblies **are stamped** with it, and the running process **exposes** it. Any one alone leaves a gap.

**Correlate product version with source revision.** SemVer alone does not identify a commit; the commit id
in `InformationalVersion` (`0.1.24-alpha+g9a7eb6c819`) and in `build-info.json` closes the loop. Ops filter
by `service.version`; engineers pivot to the commit.

**Observability is the highest-leverage surface.** OTel defines `service.version` as Stable/Recommended.
Setting it once on the resource makes every trace, metric, and log in Application Insights groupable by
version — no per-request code.

**Failure modes seen in the field:** shallow clone in CI silently yields wrong/fallback versions;
deploy-time version injection breaks reproducibility; `AssemblyVersion` displayed instead of
`InformationalVersion`; and version strings that never change so rollback forensics are impossible.

## 3. Target design

```
 git commit ──► NBGV version.json + git height ──► SemVer 0.1.24-alpha+g9a7eb6c819
      │                                                   │
      │                                    ┌──────────────┼───────────────┐
      ▼                                    ▼              ▼               ▼
  commit id                         assemblies      ADO run number   build-info.json
                                (AssemblyVersion=0.1,      = 0.1.24     (in the artifact)
                                 FileVersion=0.1.24.x,
                                 InformationalVersion=full)
                                                          │
                                                          ▼
                                          ┌──────── runtime surfaces ────────┐
                                          │ footer · /version · startup log  │
                                          │ OTel service.version             │
                                          └──────────────────────────────────┘
```

### Version string examples

| Build context | Version |
|---------------|---------|
| Committed baseline `0.1`, N commits later, topic branch | `0.1.<N>-alpha+g<sha>` |
| Release ref (matches `publicReleaseRefSpec`) | `0.1.<N>` (no commit suffix) |
| Deliberate next minor | bump `version.json` to `0.2` |
| Local build with uncommitted changes | `0.1.<N>-alpha+g<sha>-dirty` (via `gitCommitIdIncludeDirty`) |
| No git / no clone | `0.0.0-unknown` (build + pipeline guards fail) |

### Runtime JSON contract — `GET /version`

```json
{
  "version": "0.1.24-alpha",
  "informationalVersion": "0.1.24-alpha+g9a7eb6c819",
  "commit": "9a7eb6c819"
}
```

- `200` always (it is a static, non-database, host-level ops endpoint like `/health/*`).
- No secrets, paths, or environment configuration.
- Cacheable (`Cache-Control: no-cache` so a redeploy is observed immediately).

## 4. Concrete changes

### 4.1 Build / version stamping (e16s01)

| File | Change |
|------|--------|
| `version.json` (new, repo root) | `{ "version": "0.1", "publicReleaseRefSpec": [...], "cloudBuild": { "buildNumber": { "enabled": true } }, "release": { "tagName": "v{version}" } }` |
| `Directory.Packages.props` | `PackageVersion Include="Nerdbank.GitVersioning" Version="3.10.94"` |
| `Directory.Build.props` | `PackageReference` to NBGV with `PrivateAssets="all"` (repo-wide, one place) |
| `scripts/check-version.sh` (new) | Fail the pipeline if `build-info.json` is missing, empty, or `0.0.0*` |
| `Directory.Build.targets` (new) | Fail the **build** (`BeforeTargets="CoreCompile"`) when the computed version is empty or `0.0.0*` |
| `src/Weather.WebUI/Weather.WebUI.csproj` | Emit `build-info.json` into the publish output |

### 4.2 Runtime surface (e16s02)

| File | Change |
|------|--------|
| `src/Weather.WebUI/Services/AppVersion.cs` (new) | Read `AssemblyInformationalVersionAttribute` once; expose `Version`, `InformationalVersion`, `Commit`; fall back to `0.0.0-unknown` |
| `src/Weather.WebUI/Hosting/VersionEndpoints.cs` (new) | `MapGet("/version", …)` returning the record above |
| `src/Weather.WebUI/Hosting/WebUiEndpointExtensions.cs` | Map `/version` next to `/health` |
| `src/Weather.WebUI/Components/Layout/AppFooter.razor` | Show `v{Version}` immediately **before the copyright**; **remove the `.NET` `RuntimeInformation.FrameworkDescription` line**; keep the Kestrel host + OS on the left (existing tokens) |
| `src/Weather.WebUI/Components/Layout/AppHeader.razor` | Switch from `GetName().Version` to `AppVersion` (single source), or remove the duplicate |
| `Program.cs` / startup | **Console startup line** (Information, before `app.Run()`): `GeoWeather {Version} ({Commit}) starting in {Environment}` — structured properties, lands on the console; JSON in Production, human-readable in Development |

### 4.3 Observability (e16s03)

The version is surfaced **twice** in telemetry: once as the OpenTelemetry **resource attribute**
(`service.version`, automatically attached to every metric and trace) and once as an explicit
**build-info metric** anyone can group by.

| File | Change |
|------|--------|
| `src/Weather.WebUI/Telemetry/HostMeters.cs` (new) | Host meter `GeoWeather.Host` with the `geoweather.build.info` ObservableGauge (value 1; attributes `version`, `commit`) |
| `src/Weather.WebUI/Hosting/ObservabilityServiceCollectionExtensions.cs` | Add `service.name`, `service.version` resource attributes; `AddMeter(HostMeters.MeterName)` |
| `src/Weather.WebUI/Program.cs` | Console startup line (see §4.2) |
| `docs/operations/observability.md` | Document the `service.version` filter and the `geoweather.build.info` query |
| `ops/azure-monitor/workbook.json` | Optional: a version filter/parameter backed by `geoweather.build.info` |

`geoweather.build.info` contract:

| Field | Value |
|-------|-------|
| Name / type | `geoweather.build.info` / **ObservableGauge** (value always `1`) |
| Unit | `1` |
| Attributes | `version` (`0.1.24-alpha`), `commit` (short `9a7eb6c8`) — bounded, one series per deployment |
| Emitted | Once at startup; the SDK reports `1` on every collection while the process runs |
| Answers | *Which versions are live? How many instances per version? Did a deployment change?* |

Placement note: this instrument is **host-local** (`Weather.WebUI`), not in
`Weather.BuildingBlocks.Application`. It describes the host process (like the runtime instrumentation), and
BuildingBlocks must not know the application version. The host already owns the OTel SDK, so the arch rules
(no OTel in modules, no metrics in Domain) are unaffected. `version`/`commit` are bounded (change only on
deploy), so cardinality stays a function of deployments, not traffic.

### 4.4 Azure pipeline (e16s01) — the version becomes real here

The pipeline is where the computed version turns into a named, deployable artifact. `azure-pipelines.yml`
changes, and every one of them is a **coupling** that must move together:

| # | Step / area | Before | After | Why |
|---|-------------|--------|-------|-----|
| P1 | `checkout: self` | shallow clone (default) | `fetchDepth: 0` | NBGV needs full history; a shallow clone silently yields `0.0.0` |
| P2 | Run number | `$(Date:yyyyMMdd).$(Rev:r)` | NBGV sets it to the version via `##vso[build.updatebuildnumber]` | one label shared by the run, the artifact, and the app |
| P3 | Version guard | none | `scripts/check-version.sh` after publish, before the artifact upload | fail loudly instead of shipping `0.0.0` |
| P4 | CI artifact name | `artifact: 'webapp'` | `artifact: 'webapp-$(Build.BuildNumber)'` | artifact-level identity |
| P5 | Deploy download | `artifact: 'webapp'` | `artifact: 'webapp-$(Build.BuildNumber)'` | must match P4 exactly |
| P6 | Deploy package path | `$(Pipeline.Workspace)/webapp` | `$(Pipeline.Workspace)/webapp-$(Build.BuildNumber)` | must match P4/P5 |
| P7 | Build tag (optional) | none | `##vso[build.addbuildtag]$(GitBuildVersionSimple)` | filter the run history by version |
| P8 | Trigger (optional) | `main` only | add `tags: include: ['v*']` and extend the Deploy `condition` | build/deploy tagged releases |
| P9 | PR builds | CI only | version is non-public (`0.1.24-alpha+g<sha>`), run number set, no deploy | PRs stay CI-only |

Mechanics and gotchas:

- **Use `Build.BuildNumber`, not NBGV's `GitBuildVersionSimple`, for the artifact name.** NBGV publishes
  `GitBuildVersionSimple` with `task.setvariable`, which is **job-scoped** and therefore invisible to the
  separate `Deploy` job/stage. `Build.BuildNumber` is a predefined variable, so the same value resolves in
  both stages. Using the job-scoped variable to name the artifact would make the Deploy `download` fail.
- **`cloudBuild.buildNumber.includeCommitId.when: "never"`** keeps the run/artifact label clean
  (`0.1.24-alpha`) while the commit stays inside the assemblies and `build-info.json`.
- **Public refs:** only `main` (and `v*` tags) match `publicReleaseRefSpec`, so the deployed version drops
  the `+g<sha>` metadata; topic-branch and PR builds keep it. Both are valid SemVer; the run number is set
  in every case.
- **`Build.BuildNumber` is updated during `dotnet build`** (NBGV runs there), which is before publish and
  before `PublishPipelineArtifact@1` — so the artifact-name macro expands to the version.
- **Fallback if P4–P6 prove brittle:** keep the stable `artifact: 'webapp'` name and rely on the run number +
  `build-info.json` (which are already artifact-level); this removes the name coupling at the cost of a
  versioned name. Decide after the first pipeline dry-run.
- **Cache key:** the NuGet `Cache@2` key already includes `**/Directory.Packages.props`, so adding NBGV
  refills the cache once; no change needed.
- **`dotnet format` / `warnaserror`:** NBGV's generated sources live under `obj/` and are excluded from
  formatting; verified in task 1 before the rest of the story proceeds.

## 5. Test plan (offline only)

| Level | Test | Assertion |
|-------|------|-----------|
| Unit | `AppVersionTests` | Parses `0.1.24-alpha+g9a7eb6c819` into version/commit; tolerates a bare `0.1.24`; falls back to `unknown` on null/garbage; never throws |
| Unit | `AppVersionTests` | Value is the entry assembly's `InformationalVersion`, **not** `GetName().Version` |
| Unit | `HostMetersTests` | The `geoweather.build.info` observable gauge reports `1` with `version` and `commit` attributes (via `MeterListener`) |
| Integration | `StartupLogTests` | The host's startup log record carries `Version` and `Commit` (captured via a test logger provider) |
| Integration | `VersionEndpointTests` | `GET /version` → `200`, `application/json`, fields present and non-empty |
| Integration | `AppFooterTests` | `GET /` HTML contains the version immediately before the copyright, no `.NET` framework string, and still the Kestrel host + OS |
| Observability | `ObservabilityWiringTests` (extend) | Built `TracerProvider` resource contains `service.name=GeoWeather` and a non-empty `service.version`; the host meter is registered |
| Guard | `Directory.Build.targets` | Build fails when the computed version is empty or `0.0.0*` |
| Guard | `scripts/check-version.sh` | Pipeline fails when `build-info.json` is missing/empty/`0.0.0*`; covered in the story gate |
| Pipeline | `azure-pipelines.yml` YAML parse | Document parses; `Build.BuildNumber` drives artifact + deploy paths consistently |

No network, no secret, no live provider call. NBGV runs at build time only.

## 6. Rollout order

1. **e16s01** stamping + pipeline (version exists in the artifact and CI run).
2. **e16s02** runtime surface (footer + `/version` + startup log).
3. **e16s03** telemetry resource + runbook/workbook.
4. Story gate: full Preflight green on each story; pipeline dry-run on a feature branch before `main`.

## 7. Verification / acceptance

- `dotnet preflight` (build `--warnaserror`, `format --verify-no-changes`, all tests) green.
- `dotnet build -p:PublicRelease=true` yields a suffix-free version on a matching ref.
- `npm run parity:styles` remains green (footer uses existing tokens; no new CSS utility that the drift
  guard rejects — verify with `scripts/check-tailwind-css.sh` after any class change).
- Manual: run the app, confirm the footer shows the version immediately before the copyright, shows no
  `.NET` framework string, and still shows the Kestrel host + OS; the **console prints the startup line**
  with version + commit; `curl /version` matches; `curl /health/live` unaffected.
- Telemetry: with the Console exporter, confirm the resource line carries `service.version` and the
  `geoweather.build.info` gauge reports `1` with `version`/`commit`.
- CI dry-run: run number equals the version; artifact name contains the version; `build-info.json` is in
  the published zip.

## 8. Risks

| Risk | Likelihood | Mitigation |
|------|-----------|------------|
| Shallow clone → fallback version | Medium | `fetchDepth: 0` on `checkout: self`; `Directory.Build.targets` build guard + `check-version.sh` pipeline guard |
| Deploy stage can't see a job-scoped NBGV variable | Medium | Use predefined `Build.BuildNumber` (not `GitBuildVersionSimple`) for the artifact name, in both stages |
| Artifact-name coupling (P4–P6) drifts | Medium | Change all three references in the same task; dry-run the pipeline; documented fallback to a stable `webapp` name |
| NBGV generated code trips `dotnet format --verify-no-changes` or `warnaserror` | Low | Generated under `obj/`; validated in the first task before proceeding |
| Version shown before a tag exists / `0.0.0` | Low | `version.json` is committed; guards cover no-git |
| Footer duplicates the header version | Medium | Single `AppVersion` source; remove the header's `GetName().Version` |
| Informational format is not valid SemVer (NBGV uses `+g<sha>`) | Low | Display a parsed SemVer in the footer; keep the full string in `/version` |
| `geoweather.build.info` cardinality grows with deployments | Low | `version`/`commit` change only on deploy (bounded per build, not per request); old series age out; alerts key on `version` |
| Startup line leaks environment config | Low | Log only version, commit, and environment name — never keys, connection strings, or paths |
| Adding a build package into a strict `warnaserror` build | Low | Central pin; `PrivateAssets="all"`; story gate |
| Tag-triggered releases deploy from the wrong ref | Low | Only add the `tags:` trigger if tagged deploys are wanted; extend the Deploy `condition` accordingly (D9) |

## 9. Open questions / deferred

- SourceLink package (`Microsoft.SourceLink.GitHub` vs Azure Repos) — deferred until a git remote exists.
- Pre-release label convention (`alpha` vs `rc` vs no label on `main`) — see D2.
- Whether to keep the header version chip or move version entirely to the footer — see D4.
- Whether tagged (`v*`) commits should trigger a build/deploy — see D9.

## 10. Decisions to confirm

| # | Question | Recommendation | Alternative |
|---|----------|----------------|-------------|
| D1 | Version tool | **Nerdbank.GitVersioning** (`version.json` SSOT) | MinVer (tags), manual, GitVersion |
| D2 | Baseline & scheme | `"version": "0.1-alpha"` → `0.1.<height>[-alpha]`; bump `version.json` for minor/major; drop `-alpha` to stabilize | plain `0.1`; start at `1.0`; tag-driven |
| D3 | Assembly version precision | `major.minor` (stable identity) | full precision (binding churn) |
| D4 | Runtime surfaces | footer **+** `/version` **+** **console startup line** **+** OTel `service.version` **+** `geoweather.build.info` metric | footer only; footer + health enrichment |
| D5 | Displayed attribute | `InformationalVersion` (parsed) | `FileVersion`; full raw string |
| D6 | Artifact naming | Run number = version, artifact `webapp-$(Build.BuildNumber)`, `build-info.json` inside | stable `webapp` name + run number + manifest |
| D7 | SourceLink | defer (no remote) | add now |
| D8 | Fallback | `0.0.0-unknown` + build & pipeline guards | silent fallback |
| D9 | Release tags | Add a `v*` tag trigger + extend the Deploy condition so tagged releases deploy | keep `main`-only deploys; tags only for `nbgv`/history |
| D10 | Build-info metric | Host-local `GeoWeather.Host` meter; `geoweather.build.info` ObservableGauge (1) with `version`/`commit` | put it in `WeatherMeters` (couples BuildingBlocks to app version); resource attribute only |

## 11. References

- `specs/adr/0019-application-and-artifact-versioning.md`
- `specs/tech-architecture/AZURE_PIPELINE_PLAN_LATEST.md`
- `specs/tech-architecture/OBSERVABILITY_PLAN_LATEST.md`
- [OpenTelemetry service semantic conventions](https://opentelemetry.io/docs/specs/semconv/resource/service/) ·
  [OpenTelemetry metrics API](https://opentelemetry.io/docs/specs/otel/metrics/api/) ·
  [Prometheus `build_info` pattern](https://prometheus.io/docs/instrumenting/writing_exporters/#target-labels-not-static-scraped-labels)
