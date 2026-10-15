# Story e16s01 — Version stamping, artifact identity, and CI

| Field | Value |
|-------|-------|
| ID | e16s01 |
| Epic | e16 Application & Artifact SemVer (host, platform) |
| Status | planned |
| BCPs | 6 |
| Risk | P0 |
| Type | build |
| Delta | ADDED |

## 1. Summary
Introduce Nerdbank.GitVersioning with a repo-root `version.json` as the single source of truth, stamp every
assembly, tie the Azure DevOps run number and artifact name to the computed version, and emit a
self-describing `build-info.json` into the published artifact. Depends on D1–D3 and D6/D8.

## 2. Context
There is no `version.json`, no `<Version>`, and no git tag. Every assembly defaults to `1.0.0.0`; the CI run
number is the default `$(Date:yyyyMMdd).$(Rev:r)`; the `webapp` artifact carries no version. The pipeline
already uses `checkout: self`, but a shallow clone would break git-height calculation (NBGV requirement).

## 3. User story
As a release engineer, I want every build to have one semantic version that names the run, the artifact,
and the assemblies, so I can say exactly what was deployed.

## 4. Business value
Makes the artifact self-identifying and immutable; the basis for incident isolation and rollback forensics.

## 5. In scope
- `version.json` at the repo root (base `0.1-alpha`, `publicReleaseRefSpec`, `cloudBuild.buildNumber`,
  `release.tagName`, `gitCommitIdIncludeDirty`).
- NBGV pinned centrally (`Directory.Packages.props`) and referenced once in `Directory.Build.props` with
  `PrivateAssets="all"`.
- `Directory.Build.targets` build guard (fail `CoreCompile` on empty/`0.0.0*`).
- `scripts/check-version.sh` pipeline guard (fail on missing/empty/`0.0.0*` `build-info.json`).
- `build-info.json` emitted into the `dotnet publish` output.
- `azure-pipelines.yml` (the version becomes real here — every change is coupled):
  - `checkout: self` → `fetchDepth: 0` (full history; required by NBGV).
  - run number set to the version by NBGV (`cloudBuild.buildNumber.enabled`).
  - version guard after publish, before the artifact upload.
  - artifact published as `webapp-$(Build.BuildNumber)`.
  - Deploy stage `download` + `AzureWebApp@1` package path updated to the same name.
  - optional build tag `$(GitBuildVersionSimple)`; optional `v*` tag trigger (D9).

## 6. Out of scope
Runtime display (e16s02); telemetry (e16s03); SourceLink (deferred, no remote).

## 7. Domain concepts
None (build/host concern).

## 8. Layer touchpoints
| Area | Change |
|------|--------|
| repo root | `version.json` (new); `Directory.Build.targets` (new) |
| `Directory.Packages.props` | NBGV pin |
| `Directory.Build.props` | NBGV `PackageReference` |
| `scripts/` | `check-version.sh` (new) |
| `src/Weather.WebUI/Weather.WebUI.csproj` | emit `build-info.json` |
| `azure-pipelines.yml` | full clone, run number, guard, artifact name, Deploy download/package paths, optional tag trigger |

## 9. API contract
None changed.

## 10. Dependencies
None. This story unblocks e16s02 and e16s03.

## 11. Risks
| Risk | Mitigation |
|------|-----------|
| Shallow clone → wrong/fallback version | `fetchDepth: 0`; `Directory.Build.targets` build guard; `check-version.sh` pipeline guard |
| Deploy stage cannot see NBGV's job-scoped variable | Name the artifact with predefined `Build.BuildNumber`, valid in both stages |
| Artifact name P4–P6 drift out of sync | Change all three references in one task; pipeline dry-run; fallback to a stable `webapp` name |
| NBGV generated code trips `format`/`warnaserror` | Validate in task 1 before continuing |
| `Build.BuildNumber` not yet updated at publish time | NBGV update happens during `dotnet build`, which precedes publish in the job |
| Tag trigger deploys from an unintended ref | Add only if D9 is confirmed; extend the Deploy `condition` |
| NuGet restore offline during dev | Package is restored from nuget.org like the rest; CI already restores |

## 12. Requirements (delta)
- **ADDED** — repository has a committed base version and stamps `AssemblyVersion` (`major.minor`),
  `FileVersion` (full), and `InformationalVersion` (full SemVer + `+g<sha>`).
- **ADDED** — the Azure DevOps run number equals the computed version.
- **ADDED** — the published artifact is named with the version and contains `build-info.json`.
- **MODIFIED** — the pipeline checks out full history and deploys the versioned artifact by a single
  cross-stage variable (`Build.BuildNumber`), with no version injection at deploy time.

## 13. Implementation steps
1. Add `version.json` (base `0.1-alpha`); pin NBGV centrally and reference it in `Directory.Build.props`; verify a clean build and that `dotnet format --verify-no-changes` and `warnaserror` stay green with the generated `ThisAssembly` sources → verify: `dotnet build Weather.sln --warnaserror && dotnet format Weather.sln --verify-no-changes`
2. Add `Directory.Build.targets` (fail `CoreCompile` when the version is empty/`0.0.0*`) → verify: `dotnet build Weather.sln --warnaserror`
3. Emit `build-info.json` into the publish output from `Weather.WebUI.csproj` → verify: `dotnet publish src/Weather.WebUI -o /tmp/publish && cat /tmp/publish/build-info.json`
4. Add `scripts/check-version.sh` (reads `build-info.json`; fails on missing/empty/`0.0.0*`) → verify: `bash scripts/check-version.sh /tmp/publish/build-info.json`
5. Update `azure-pipelines.yml`: `checkout: self` + `fetchDepth: 0`; run the guard after publish; keep the rest of the order unchanged → verify: `python3 -c "import yaml;yaml.safe_load(open('azure-pipelines.yml'))"`
6. Rename the CI artifact to `webapp-$(Build.BuildNumber)` and update the Deploy `download` + `AzureWebApp@1` package path to match (all three in one commit) → verify: pipeline dry-run on a feature branch
7. Story gate: full preflight green; run number equals the version; artifact name + `build-info.json` carry the version → verify: `dotnet build Weather.sln --warnaserror && dotnet format Weather.sln --verify-no-changes && dotnet test Weather.sln`

## 14. Test plan
- Build assertions: `AssemblyInformationalVersion` contains `+` and a commit id; `AssemblyVersion` is
  `0.1.0.0` while `InformationalVersion` differs per build.
- Build guard: a forced `NBGV_...`/no-git condition fails `CoreCompile`.
- Pipeline guard: `check-version.sh` exits non-zero on a missing/`0.0.0` `build-info.json`.
- Publish: `build-info.json` exists and contains `version`, `informationalVersion`, `commit`.
- Pipeline YAML parses; the artifact name and the Deploy package path both reference `$(Build.BuildNumber)`.
- Dry-run: run number = version; artifact name carries the version.
- Pipeline YAML parses; run number source documented.

## 15. Acceptance criteria (Gherkin)
```gherkin
Scenario: A build carries a semantic version
  Given a clean full clone
  When the solution is built
  Then every assembly's InformationalVersion is major.minor.patch[-pre]+g<sha>
  And the AssemblyVersion is only major.minor

Scenario: A version cannot be silently unknown
  Given the computed version is empty or 0.0.0
  When the build guard runs
  Then the build fails
```

## 16. Definition of done
`version.json` committed; NBGV stamps assemblies; run number and artifact name carry the version;
`build-info.json` is in the artifact; guard green; Preflight green.

## 17. Verification script
1. `bash scripts/check-version.sh`
2. `dotnet publish src/Weather.WebUI -o /tmp/publish && cat /tmp/publish/build-info.json`
3. Inspect `AssemblyVersion`/`InformationalVersion` with a one-off `dotnet` reflection snippet.
4. Dry-run the pipeline on a feature branch; confirm the run number and artifact name.

## 18. Risks / notes
NBGV falls back to `0.0.0` only when git is unavailable; the guard makes that loud rather than silent.

## 19. Sizing / BCP rationale
5 BCPs — version file + package wiring + guard + publish manifest + pipeline.

## 20. References
- `specs/adr/0019-application-and-artifact-versioning.md` (D1–D3, D6, D8)
- `specs/tech-architecture/VERSIONING_PLAN_LATEST.md` §4.1, §4.4
