# Story e16s03 — Telemetry correlation (OTel `service.version` + `geoweather.build.info` metric)

| Field | Value |
|-------|-------|
| ID | e16s03 |
| Epic | e16 Application & Artifact SemVer (host, platform) |
| Status | planned |
| BCPs | 4 |
| Risk | P0 |
| Type | feat |
| Delta | MODIFIED |

## 1. Summary
Put the version into telemetry two ways: (1) as the OpenTelemetry **resource** attributes `service.name` and
`service.version`, which automatically tag every metric, trace, and log; and (2) as an explicit
**`geoweather.build.info`** ObservableGauge (value `1`, attributes `version` and `commit`) on a host-local
meter, so the version is a queryable, groupable metric. Document the filter in the observability runbook and
optionally add a version parameter to the workbook. Depends on D4/D10.

## 2. Context
`ObservabilityServiceCollectionExtensions.ResourceAttributes` currently sets `deployment.environment` and
`cloud.provider` only. OpenTelemetry defines `service.version` as Stable/Recommended and `service.name` as
Required; Application Insights uses them for the Cloud Role Name and filtering. ADR-0014 places business
instruments in `Weather.BuildingBlocks.Application` (`WeatherMeters`); this story adds a **host-local** meter
(`GeoWeather.Host`) because a build-info gauge describes the host process and BuildingBlocks must not know the
application version — the same rationale as `AddRuntimeInstrumentation()`. `MeterListener` (BCL) is the
established offline test mechanism (`ApiErrorMetricsTests`).

## 3. User story
As an on-call engineer, I want to filter telemetry by app version and count instances per version, so I can
tell whether an error spike or a rollout started with the new build.

## 4. Business value
Turns version into a first-class telemetry dimension: deploy-to-incident correlation, canary/rollout
monitoring, and version-drift alerts — without redeploying or guessing.

## 5. In scope
- `Telemetry/HostMeters.cs` (new): host meter `GeoWeather.Host`; `geoweather.build.info` ObservableGauge
  (value `1`, attributes `version`, `commit`) sourced from `AppVersion`.
- `ObservabilityServiceCollectionExtensions`: add `service.name = "GeoWeather"` and
  `service.version = <informational version>` resource attributes; `AddMeter(HostMeters.MeterName)`.
- `ObservabilityWiringTests`: assert the resource attributes and that the host meter is registered.
- `HostMetersTests`: assert the gauge reports `1` with `version`/`commit` via `MeterListener`.
- `docs/operations/observability.md`: document the `service.version` filter and the `geoweather.build.info`
  query; optional workbook version parameter.

## 6. Out of scope
New business counters; changing sampling; EF Core instrumentation; log-scope version enrichment (later
follow-up if needed); a build-age metric (possible follow-up).

## 7. Domain concepts
None. Modules never reference an OpenTelemetry package (arch-test enforced); Domain has no metrics.

## 8. Layer touchpoints
| Assembly | Change |
|----------|--------|
| Weather.WebUI | `Telemetry/HostMeters.cs` (new); `Hosting/ObservabilityServiceCollectionExtensions.cs`; `Program.cs` (startup line, e16s02) |
| Weather.WebUI.UnitTests | `Observability/ObservabilityWiringTests.cs`; `Telemetry/HostMetersTests.cs` (new) |
| docs | `docs/operations/observability.md`; `ops/azure-monitor/workbook.json` (optional) |

## 9. API contract
None changed. Metric contract added: `geoweather.build.info` (gauge `1`, attributes `version`, `commit`).

## 10. Dependencies
e16s01 (a real version must exist) and e16s02 (`AppVersion` is the reader).

## 11. Risks
| Risk | Mitigation |
|------|-----------|
| Cardinality growth | `version`/`commit` change only on deploy (bounded per build, not per request); documented; alert on `version` |
| Tests need a live exporter | `Observability:Enabled=false` keeps the offline path; tests use `MeterListener` and the built provider, no socket |
| Instrument placement violates ADR-0014 | Host-local meter, documented as host-process info (like runtime instrumentation); modules and Domain untouched; arch tests unaffected |
| Version unavailable when telemetry is off | Resource + gauge are configured only when enabled; outside Development telemetry is always enabled |

## 12. Requirements (delta)
- **MODIFIED** — the OTel resource carries `service.name` and `service.version` in addition to
  `deployment.environment` and `cloud.provider`.
- **ADDED** — the host emits `geoweather.build.info` (ObservableGauge `1`, attributes `version`, `commit`).

## 13. Implementation steps
1. Add `Telemetry/HostMeters.cs` (meter + gauge using `AppVersion`) → verify: `dotnet test Weather.sln --filter Category=HostMeters`
2. Add `service.name`/`service.version` to `ResourceAttributes` and `AddMeter(HostMeters.MeterName)` → verify: `dotnet test Weather.sln --filter Category=Observability`
3. Extend `ObservabilityWiringTests` (resource attributes present, host meter registered) and add `HostMetersTests` (gauge = 1 with both attributes) → verify: `dotnet test Weather.sln --filter "Category=Observability|Category=HostMeters"`
4. Document the version filter + `geoweather.build.info` in `docs/operations/observability.md` (and workbook, optional) → verify: `dotnet build Weather.sln --warnaserror`
5. Story gate: full preflight green → verify: `dotnet build Weather.sln --warnaserror && dotnet format Weather.sln --verify-no-changes && dotnet test Weather.sln`

## 14. Test plan
- Unit `HostMetersTests`: `MeterListener` sees one `geoweather.build.info` measurement equal to `1` with
  `version` and `commit` tags.
- Unit `ObservabilityWiringTests`: `Enabled=true` builds providers whose resource contains
  `service.name=GeoWeather` and a non-empty `service.version`; the host meter name is registered;
  `Enabled=false` builds no provider.
- Manual (documented): Console exporter shows the resource `service.version` and the gauge.

## 15. Acceptance criteria (Gherkin)
```gherkin
Scenario: Telemetry is version-tagged
  Given observability is enabled
  When the OpenTelemetry resource is built
  Then service.name is GeoWeather
  And service.version is the stamped informational version

Scenario: The version is a queryable metric
  When the host meter is observed
  Then geoweather.build.info reports 1 with version and commit attributes

Scenario: No telemetry is created when disabled
  Given observability is disabled
  When the service provider is built
  Then no MeterProvider or TracerProvider exists
```

## 16. Definition of done
Resource carries `service.name`/`service.version`; `geoweather.build.info` reports `1` with `version`/`commit`;
tests assert both; runbook documents the filter; Preflight green.

## 17. Verification script
1. `dotnet run --project src/Weather.WebUI` with `Observability__ExportTarget=Console`.
2. Confirm the resource line includes `service.version` and the gauge reports `1` with `version`/`commit`.
3. `dotnet test Weather.sln --filter "Category=Observability|Category=HostMeters"`.

## 18. Risks / notes
`service.instance.id` is left to the distro default; the gauge's `version` is the parsed SemVer while the
resource `service.version` is the full informational version (both are useful; the resource ties every signal
to the exact build).

## 19. Sizing / BCP rationale
4 BCPs — host meter + resource attributes + test updates + docs/workbook.

## 20. References
- `specs/adr/0019-application-and-artifact-versioning.md` (D4, D10)
- `specs/tech-architecture/VERSIONING_PLAN_LATEST.md` §4.3
- `specs/tech-architecture/OBSERVABILITY_PLAN_LATEST.md` §5.5
- `docs/operations/observability.md`
- [OpenTelemetry service semantic conventions](https://opentelemetry.io/docs/specs/semconv/resource/service/)
