# Story e12s01 — Telemetry foundation & host wiring

| Field | Value |
|-------|-------|
| ID | e12s01 |
| Epic | e12 Observability |
| Status | todo |
| BCPs | 5 |
| Risk | P0 |
| Type | feat |
| ADR | ADR-0014 |

## 1. Summary
Introduce OpenTelemetry in the single host: add the Microsoft OpenTelemetry Distro, a shared telemetry
primitive in `Weather.BuildingBlocks.Application`, configuration-gated host wiring that exports to Azure
Monitor, health endpoints, and a hard offline guarantee for tests. No business metrics yet — this story makes
the pipeline observable and safe.

## 2. Context
The app emits only console logs today and the RFC 9457 `traceId` has no backing trace. ADR-0014 selects the
Microsoft OpenTelemetry Distro (`Microsoft.OpenTelemetry` 1.1.0) exporting to Azure Monitor. Modules must not
gain an OpenTelemetry dependency, and automated tests must stay offline. `ProblemDetailsCustomization` already
uses `Activity.Current?.Id`; it must be aligned to the W3C trace id.

## 3. User story
As an operator, I want the host to emit OpenTelemetry telemetry to Azure Monitor when enabled, so I can observe
the service without any module being coupled to a vendor SDK.

## 4. Business value
Establishes the observability foundation the operations team requires before the app is deployed to App Service.

## 5. In scope
- `Microsoft.OpenTelemetry` 1.1.0 + `OpenTelemetry.Instrumentation.Runtime` in `Directory.Packages.props` and
  `Weather.WebUI.csproj`.
- `ObservabilityOptions` (`Enabled`, `ExportTarget`, `SamplingRatio`, `TracesPerSecond`) bound and validated.
- `WeatherMeters` (`Meter("Weather")`, instrument definitions, tag-key constants) and `WeatherActivity`
  (`ActivitySource("Weather.Activity")`) in `Weather.BuildingBlocks.Application`.
- Host wiring in `WebUiServiceCollectionExtensions`: `UseMicrosoftOpenTelemetry` when enabled; `AddMeter`
  ("Weather") + `AddSource` ("Weather.Activity"); runtime instrumentation; resource attributes
  (`deployment.environment`, `cloud.provider=azure`).
- `/health/live` and `/health/ready`.
- `ProblemDetailsCustomization` uses `Activity.Current?.TraceId.ToHexString()`.
- Test host sets `Observability:Enabled=false`; arch guards; `Observability` integration test category.

## 6. Out of scope
- Any custom business instrument emission (e12s02/e12s03).
- EF Core / SQLite instrumentation (ADR-0014 D3).
- Dashboard and alert artifacts (e12s04).

## 7. Domain concepts
None.

## 8. Layer touchpoints
| Assembly | Change |
|----------|--------|
| `Weather.BuildingBlocks.Application` | `Telemetry/WeatherMeters.cs`, `Telemetry/WeatherActivity.cs` |
| `Weather.WebUI` | `Configuration/ObservabilityOptions.cs`, host wiring, `/health/*`, ProblemDetails customization |
| `Weather.IntegrationTests` | test config (`Observability:Enabled=false`), `Observability*Tests`, `Health*Tests` |
| `Weather.ArchTests` | guards (Domain free of metrics; modules free of OTel packages) |

## 9. API contract
Adds `GET /health/live` (200) and `GET /health/ready` (200/503). No change to existing routes or bodies.
`traceId` stays present in Problem Details; its value becomes the OTel trace id (tests assert non-empty only).

## 10. Dependencies
None. First story of e12.

## 11. Risks
| Risk | Mitigation |
|------|-----------|
| Package not restorable | `Microsoft.OpenTelemetry` is GA on nuget.org (1.1.0); pinned centrally |
| Telemetry accidentally enabled in tests | `Observability:Enabled=false` in the test host + guard test that no meter provider/exporter is created |
| Connection string logged | never bind it to a logged options class; read only via env var; security-tagged task |
| Runtime metrics absent (distro omits them) | explicit `OpenTelemetry.Instrumentation.Runtime` registration |
| Sampling default (5/s) silently wins | explicitly set `TracesPerSecond = null` before `SamplingRatio` |
| Health endpoint becomes an internal probe | `/health/ready` performs no writes and returns no data |

## 12. Implementation steps
1. Add `Microsoft.OpenTelemetry` 1.1.0 + `OpenTelemetry.Instrumentation.Runtime` to central package management and the host → verify: `dotnet build Weather.sln --warnaserror`
2. Add `ObservabilityOptions` bound + validated → verify: `dotnet test src/Tests/Weather.WebUI.UnitTests`
3. Add `WeatherMeters` + `WeatherActivity` primitives (names, tag keys, instrument definitions) → verify: `dotnet build Weather.sln --warnaserror`
4. Wire `UseMicrosoftOpenTelemetry` gated by config, with `AddMeter`/`AddSource`/runtime instrumentation and resource attributes → verify: `dotnet build Weather.sln --warnaserror`
5. Point the test host at `Observability:Enabled=false`; add arch guards → verify: `dotnet test Weather.sln`
6. Align `ProblemDetailsCustomization` to `Activity.Current?.TraceId.ToHexString()` → verify: `dotnet test src/Tests/Weather.IntegrationTests --filter Category=ProblemDetails`
7. Add `/health/live` + `/health/ready` → verify: `dotnet test src/Tests/Weather.IntegrationTests --filter Category=Health`
8. Add `Observability` tests: disabled ⇒ no exporter; meter/source registered by name when enabled → verify: `dotnet test src/Tests/Weather.IntegrationTests --filter Category=Observability`
9. Full preflight → verify: `dotnet build Weather.sln --warnaserror && dotnet format Weather.sln --verify-no-changes && dotnet test Weather.sln`

## 13. Test plan
| Scenario | Level | Risk |
|----------|-------|------|
| `SC-e12s01-P0-01` Telemetry disabled ⇒ no meter/tracer provider or exporter is constructed | Integration | P0 |
| `SC-e12s01-P0-02` Domain assemblies reference no `System.Diagnostics.Metrics`; modules reference no OTel package | Arch | P0 |
| `SC-e12s01-P1-01` `Weather` meter and `Weather.Activity` source are registered when enabled | Integration | P1 |
| `SC-e12s01-P1-02` `/health/live` returns 200; `/health/ready` returns 200 with contexts reachable | Integration | P1 |
| `SC-e12s01-P1-03` ProblemDetails `traceId` equals `Activity.Current.TraceId` and `X-Correlation-ID` | Integration | P1 |

## 14. Acceptance criteria (Gherkin)
```gherkin
Scenario: Telemetry is off in tests
  Given Observability:Enabled is false
  Then no Azure Monitor exporter or OpenTelemetry SDK is created
  And the host responds normally

Scenario: Telemetry is on in the app
  Given Observability:Enabled is true
  When the host starts
  Then the "Weather" meter and "Weather.Activity" source are registered with the provider

Scenario: Health probes
  When I GET /health/live
  Then the response is 200
  When I GET /health/ready
  Then the response is 200 when the module databases are reachable

Scenario: traceId maps to a trace
  When an API request fails
  Then the Problem Details traceId equals the current Activity trace id
  And the X-Correlation-ID header matches
```

## 15. Definition of done
Build (`--warnaserror`), format, and `dotnet test Weather.sln` green; telemetry disabled in tests; arch guards
enforced; health endpoints live; ADR-0014 referenced.

## 16. Verification script
1. `dotnet build Weather.sln --warnaserror`
2. `dotnet test src/Tests/Weather.IntegrationTests --filter Category=Observability`
3. `dotnet test src/Tests/Weather.IntegrationTests --filter Category=Health`
4. `dotnet run --project src/Weather.WebUI` → `curl -i /health/live`, `curl -i /health/ready`
5. Set `APPLICATIONINSIGHTS_CONNECTION_STRING` + `Observability__Enabled=true` in a scratch environment and
   confirm the app starts and emits (manual, out of CI).

## 17. Out of scope
Section 6.

## 18. Risks / notes
The distro is always-instrumenting; the only reliable offline guarantee is to not call
`UseMicrosoftOpenTelemetry` at all when disabled. Guard that.

## 19. Sizing / BCP rationale
5 BCPs — two packages, one options class, two telemetry primitives, host wiring, health endpoints, test/arch
guards.

## 20. References
- `specs/tech-architecture/OBSERVABILITY_PLAN_LATEST.md` §§3, 7, 8, 9
- `specs/adr/0014-opentelemetry-observability.md`
