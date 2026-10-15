# Observability Plan — OpenTelemetry

> Status: **IMPLEMENTED (e12s01–e12s04).** Decisions **LOCKED** (D1–D5, see §13). §14 decisions record updated where implementation differed (dev telemetry is opt-in via `launchSettings.json`; no EF Core instrumentation).
> Decision record: `specs/adr/0014-opentelemetry-observability.md`.
> Epic capsule: `specs/epics/e12-observability/`. Registered in `specs/release-plan.yaml`.
> This file is the metrics/approach authority for e12.

## 1. Goal and operating scenarios

Give an operations team a single OpenTelemetry pipeline that answers two classes of question:

**Technical (is the service healthy?)**
- Is the app up and are requests succeeding? (RED — Rate, Errors, Duration)
- Is the outbound OpenWeatherMap dependency slow or failing?
- Is the process (GC, thread pool, memory, CPU) under pressure?
- Are MediatR handlers slow or failing? Are events-bus handlers failing?

**Business (is the product being used and working?)**
- How many weather lookups happen, and what share succeed vs. 404 vs. provider error?
- How many favorites and notes are created, and what share fail validation or duplicate?
- Which countries are explored most (bounded attributes only)?
- Are integration events (Geography → Journal projection) flowing?

User-facing surfaces (host `Weather.WebUI`, single process per ADR-0012):

| Endpoint | Owning module | Business intent |
|----------|---------------|-----------------|
| `GET /api/countries` | Geography | Browse catalogue |
| `GET /api/countries/{countryCode}/cities` | Geography | Select a city |
| `GET /api/weather/{cityName}` | Weather | Read current weather |
| `POST /api/weather/notes` | Journal (notes) | Save a weather note |
| `POST /api/cities/favorites` | Journal (favorites) | Save a favorite city |
| `GET /api/cities/favorites` | Journal (favorites) | List favorites |
| Blazor routes (`/`, `/favorites`) | WebUI | UI usage |

### Non-goals
- No user analytics, PII, or session tracking (no auth in the app).
- No log-aggregation product beyond Azure Monitor.
- No replacement of the existing `ILogger<T>` structured logging.
- No EF Core query-level tracing (D3 = skip).

## 2. Signal strategy

| Signal | Role | Sampling |
|--------|------|----------|
| **Metrics** | Primary artifact for the ops team. Always on, aggregated, cheap. Both technical and business. | Never sampled |
| **Traces** | Root-cause: one trace per HTTP request spanning controller → MediatR → OpenWeatherMap and the events bus. Drill-down from a failing metric. | 100% dev / 10% prod (D4) |
| **Logs** | Existing `ILogger<T>` messages, exported to Azure Monitor and correlated by `trace_id`/`span_id`. | Volume-based |

`traceId` already surfaces in RFC 9457 errors (ADR-0011). Steps must make it equal the OTel trace id so a
support ticket maps 1:1 to a trace.

## 3. Architecture & placement (modular-monolith constraints)

Hard rules: modules never reference each other, Domain has no framework dependency, the host is the
composition root.

```
Weather.WebUI (host)                       ← UseMicrosoftOpenTelemetry(), ExportTarget, sampling, /health
   └── AddMeter("Weather") + AddSource("Weather.Activity")  (register by name)
Weather.BuildingBlocks.Application         ← telemetry primitives (single source of truth)
   └── WeatherMeters (static Meter + instrument definitions + tag-key constants)
   └── WeatherActivity (ActivitySource)
   └── TelemetryBehavior<TRequest,TResponse>  ← MediatR pipeline (mirrors existing ValidationBehavior)
Weather.Modules.<Module>.{Application,Infrastructure}
   └── call the shared instruments inline in handlers/services/providers
```

**Reason for the shared primitives (Complexity Pushback):** instrument *names, units, and tag keys* must be
identical wherever they are emitted; one static holder guarantees that without an interface per metric.
No `IObservability` interface is introduced — instruments are static and testable via `MeterListener`, so an
interface would add a seam with no test benefit.

- **Exporters/SDK live only in the host.** Modules emit into the process-global `Meter`/`ActivitySource`; the
  host subscribes (`AddMeter`/`AddSource`) and exports. Arch test forbids `Microsoft.OpenTelemetry` and any
  `OpenTelemetry.*` package in module projects.
- **Domain stays clean:** no `System.Diagnostics.Metrics` usage in any `*.Domain` assembly.
- Metrics emitted with no SDK attached are a no-op — nothing in a module knows the exporter choice.

## 4. Resource and conventions

Resource attributes (distro sets `service.name`/version/instance automatically; add the rest):
`deployment.environment` (= `ASPNETCORE_ENVIRONMENT`), `cloud.provider=azure`, `cloud.region` (config when
deployed). Distro resource detectors add App Service / VM / Container Apps attributes automatically.

Conventions: OpenTelemetry semantic conventions for built-in instruments; custom instruments use the
`weather.*` / `<module>.*` dotted namespace, singular nouns, `{request}`-style units.

### Cardinality guardrail (mandatory)
Never use unbounded values as metric attributes: **no city name, note text, raw URL/path, ids, exception
message, or query string**. Allowed: closed enums, country codes, HTTP methods, route templates, module/handler
names, status codes. High-cardinality detail belongs on spans.

## 5. Metric catalog

### 5.1 Technical — HTTP RED (distro-supplied: ASP.NET Core + HttpClient)
`http.server.request.duration`, `http.server.active_requests`, `http.server.request.body.size`,
`http.server.response.body.size`, `http.client.request.duration`, `http.client.active_requests`.
Covers OWM (`OpenWeatherMapService`) and the Blazor `WeatherApiClient` outbound calls.

### 5.2 Runtime & platform
- Application Insights standard metrics + perf counters (distro: `EnableStandardMetrics`,
  `EnablePerfCounters`): CPU, memory, request rate, dependency rate.
- GC / thread-pool: add **`OpenTelemetry.Instrumentation.Runtime`** and register it with
  `ConfigureOpenTelemetryMeterProvider(b => b.AddRuntimeInstrumentation())` — the distro's built-in metric list
  covers only `Microsoft.AspNetCore.Hosting` and `System.Net.Http`, so runtime metrics are an explicit add:
  `dotnet.gc.collections`, `dotnet.gc.heap.total_allocated`, `dotnet.gc.last_collection.heap.size`,
  `dotnet.gc.pause.time`, `dotnet.thread_pool.thread.count`, `dotnet.thread_pool.queue.length`,
  `dotnet.process.cpu.time`, `dotnet.process.memory.working_set`, `dotnet.jit.compiled_il.size`.
- Kestrel queue pressure via ASP.NET Core metrics / EventCounters (best-effort).

### 5.3 Data & messaging
| Metric | Type | Unit | Attributes | Source |
|--------|------|------|------------|--------|
| `mediatr.requests` | Counter | {request} | mediatr.request.name, mediatr.request.kind (query\|command), module, mediatr.outcome | `TelemetryBehavior` |
| `mediatr.request.duration` | Histogram | ms | same | `TelemetryBehavior` |
| `integration.event.published` | Counter | {event} | event.type | `InMemoryEventsBus.PublishAsync` |
| `integration.event.handled` | Counter | {event} | event.type, handler.module, integration.outcome | `InMemoryEventsBus.PublishAsync` |
| `integration.event.failed` | Counter | {event} | event.type, handler.module, error.type | `InMemoryEventsBus.PublishAsync` |
| `integration.event.duration` | Histogram | ms | event.type, handler.module | `InMemoryEventsBus.PublishAsync` |

> No `db.client.*` metrics: EF Core instrumentation is out of scope (D3). DB visibility from logs + MediatR timing.

### 5.4 Business metrics (custom)
| Metric | Type | Unit | Attributes (bounded) | Emitted at | Business question |
|--------|------|------|----------------------|------------|-------------------|
| `weather.report.requests` | Counter | {request} | weather.outcome = success \| not_found \| provider_error \| validation_error; geo.country_code | `WeatherService` / `GetCurrentWeatherQueryHandler` | Lookup volume + success rate |
| `weather.report.duration` | Histogram | ms | weather.outcome | same | End-to-end lookup latency |
| `weather.provider.requests` | Counter | {request} | http.response.status_code, weather.outcome | `OpenWeatherMapService` | Upstream call volume |
| `weather.provider.failures` | Counter | {failure} | error.type = timeout \| unavailable \| bad_status \| empty_body | `OpenWeatherMapService` | Dependency health / 502 predictor |
| `weather.provider.duration` | Histogram | ms | http.response.status_code | `OpenWeatherMapService` | Upstream latency |
| `weather.city.not_found` | Counter | {request} | geo.country_code | `GetCurrentWeatherQueryHandler` | Catalogue coverage gap |
| `geography.country.list_requests` | Counter | {request} | geo.has_search (bool) | Geography service | Catalogue usage |
| `geography.city.list_requests` | Counter | {request} | geo.country_code | Geography service | Which countries are explored |
| `favorites.operations` | Counter | {operation} | operation = add \| list; favorites.outcome = created \| duplicate \| city_not_found \| invalid \| success | Journal favorites service | Save funnel + failure mix |
| `favorites.duration` | Histogram | ms | operation, favorites.outcome | Journal favorites service | Write/read latency |
| `favorites.saved.count` | ObservableGauge | {favorite} | — | JournalContext | Business KPI: saved favorites |
| `notes.operations` | Counter | {operation} | notes.outcome = created \| city_not_found \| invalid | Journal notes service | Note-writing funnel |
| `notes.duration` | Histogram | ms | notes.outcome | Journal notes service | Write latency |
| `notes.saved.count` | ObservableGauge | {note} | — | JournalContext | Business KPI: saved notes |
| `api.errors` | Counter | {error} | error.code (six stable codes), http.response.status_code, http.request.method, http.route | `GlobalExceptionHandler` + malformed-request path | Error taxonomy over time |

**Derived indicators (dashboards, not instruments):** lookup→favorite conversion
(`favorites.operations{outcome=created}` ÷ `weather.report.requests{outcome=success}`), upstream error ratio,
catalogue not-found ratio, favorites/notes growth per day.

### 5.5 Error taxonomy
`api.errors{error.code}` reuses ADR-0011's six codes — `MALFORMED_REQUEST`, `VALIDATION_FAILED`,
`RESOURCE_NOT_FOUND`, `DUPLICATE_RESOURCE`, `DEPENDENCY_UNAVAILABLE`, `INTERNAL_ERROR` — so technical error rate
and business failure mix share one vocabulary.

## 6. Traces

| Span | Where | Key attributes |
|------|-------|----------------|
| Server span (distro) | ASP.NET Core | http.route, http.response.status_code, trace id = ProblemDetails traceId |
| Client span (distro) | OWM + `WeatherApiClient` | server.address, http.response.status_code |
| `mediatr <request>` | `TelemetryBehavior` | request name, kind, module, outcome |
| `weather.provider.get_current` | `OpenWeatherMapService` | geo.country_code, weather.outcome |
| `integration.publish` / `integration.handle <event>` | `InMemoryEventsBus` | event.type, handler.module |
| `geography.catalog.publish` | `CityCatalogEventPublisher` | city count |

## 7. Config, health, and environment

- Toggle: `Observability:Enabled` (default **false in tests**; true in Development/Production config). When
  false, `UseMicrosoftOpenTelemetry()` is not called → no SDK, no exporters, no sockets.
- Export target: `Observability:ExportTarget` = `AzureMonitor` (prod) | `Console` (local debug) | `Otlp`.
- Connection string: `APPLICATIONINSIGHTS_CONNECTION_STRING` env var (App Service config / Key Vault
  reference). Never in `appsettings.json`, never logged (CONVENTIONS §Key handling).
- Sampling (D4): dev `SamplingRatio=1.0, TracesPerSecond=null`; prod `SamplingRatio=0.1, TracesPerSecond=null`.
  **`TracesPerSecond` defaults to 5.0 and takes precedence over `SamplingRatio` — it must be explicitly nulled.**
  Env alternatives: `OTEL_TRACES_SAMPLER=microsoft.fixed_percentage` + `OTEL_TRACES_SAMPLER_ARG`.
- Custom signals: `ConfigureOpenTelemetryMeterProvider(b => b.AddMeter("Weather"))` and
  `ConfigureOpenTelemetryTracerProvider(b => b.AddSource("Weather.Activity"))`.
- Health endpoints (currently absent): `GET /health/live` (process) and `GET /health/ready` (EF contexts
  reachable). App Service health check + uptime monitoring consume these; they also feed HTTP metrics.

## 8. Package decisions (slopcheck)

| Package | Tag | Rationale |
|---------|-----|-----------|
| `Microsoft.OpenTelemetry` **1.1.0** | [OK] | Microsoft OpenTelemetry distro (GA, net8.0/netstandard2.0). One call wires traces+metrics+logs, ASP.NET Core + HttpClient + Azure Monitor exporter + resource detectors. Microsoft's documented path for a new OTel adoption. Supersedes `Azure.Monitor.OpenTelemetry.AspNetCore`. |
| `OpenTelemetry.Instrumentation.Runtime` | [OK] | Official OTel .NET; only for `dotnet.*` GC/thread-pool metrics (§5.2). |
| `OpenTelemetry.Exporter.InMemory` | [OK] | Test-only, and only if `MeterListener`/`ActivityListener` (BCL) prove insufficient. Prefer BCL listeners. |
| `Azure.Monitor.OpenTelemetry.AspNetCore` | [OK] | Legacy distro; **not** used (superseded) unless the team already standardises on it. |
| `OpenTelemetry.Instrumentation.EntityFrameworkCore` | [SUS] | Official but pre-1.0; **rejected** — D3 = skip. |

Add versions to `Directory.Packages.props` (central management). No `[SLOP]` packages. No direct
`OpenTelemetry.*` instrumentation needed for ASP.NET Core/HttpClient — the distro covers them.

## 9. Test strategy (offline, deterministic)

- `Observability:Enabled=false` in the test host → `UseMicrosoftOpenTelemetry()` is never called → **no exporter,
  no sockets** (satisfies the offline rule).
- Metric assertions via `System.Diagnostics.Metrics.MeterListener` (BCL), attached to the static `Weather` meter
  — works with or without the SDK.
- Trace assertions via `ActivityListener`; assert the `traceId` in a ProblemDetails response matches
  `Activity.Current?.Id`.
- New arch tests: no `*.Domain` assembly references `System.Diagnostics.Metrics`; no module project references
  `Microsoft.OpenTelemetry` or any `OpenTelemetry.*` package (exporters are host-only).
- New integration test category `Observability`; metric emission asserted per success/failure/duplicate path.

## 10. SLOs and alerts (proposed)
| Signal | Objective | Alert (sustained 5–10 min) |
|--------|-----------|----------------------------|
| Availability | 99.5% of `/api/*` non-5xx | error-rate > 1% |
| Latency | `/api/weather/*` p95 < 1500 ms; other `/api/*` p95 < 400 ms | p95 breach |
| Dependency | OWM 502 share < 2% | `weather.provider.failures` rate spike |
| Business | no catastrophic favorite-create failure | `favorites.operations{outcome=created}` = 0 over 1 h with traffic |
| Integration | events handled = published | `integration.event.failed` > 0 |

Alerts are delivered in Azure Monitor (D1=a). See §13 D5.

## 11. Proposed epic decomposition

**e12-observability** — WSJF 6.5, total BCPs 21. Full specs and tasks are in
`specs/epics/e12-observability/`.

### e12s01 — Telemetry foundation & host wiring (BCP 5, P0)
Distro package + `ObservabilityOptions` (Enabled, ExportTarget, sampling) bound/validated; shared
`WeatherMeters`/`WeatherActivity` primitives in BuildingBlocks; `UseMicrosoftOpenTelemetry()` wiring with
`AddMeter`/`AddSource`/runtime instrumentation; resource attributes; `traceId` parity with ProblemDetails;
`/health/live` + `/health/ready`; exporters disabled in tests; arch guards.

### e12s02 — Technical instrumentation (BCP 5, P1)
`TelemetryBehavior` MediatR pipeline (requests/duration/span); events-bus instrumentation; OWM provider
instrumentation; log enrichment (`trace_id`/`span_id`); MeterListener assertions.

### e12s03 — Business metrics (BCP 6, P0)
Weather, Geography, Journal (favorites + notes) metrics; `favorites.saved.count` / `notes.saved.count` gauges;
`api.errors`; business-path assertions.

### e12s04 — Operations: dashboards, alerts, runbook (BCP 5, P1)
Azure Monitor Workbook JSON + alert-rule templates as repo artifacts; observability runbook; AGENTS/CONVENTIONS/
tech-stack/TEST_PLAN updates; ADR-0014 + capsule registration; preflight.

## 12. Risks
| Risk | Mitigation |
|------|-----------|
| Metrics leak PII / unbounded labels | §4 cardinality guardrail; review each instrument; code-review checklist item |
| Azure Monitor ingestion cost from high volume | metrics aggregated; traces sampled (D4); `TracesPerSecond` explicit |
| Runtime metrics missing (distro omits them) | add `OpenTelemetry.Instrumentation.Runtime` explicitly (§5.2) |
| Tests accidentally open sockets | `Observability:Enabled=false` in test config + guard arch test |
| Cross-module telemetry becomes a back-door dependency | instruments live in BuildingBlocks only; arch test forbids OTel packages in modules |
| Sampling hides failures | metrics are never sampled; alert on metrics, drill with traces |
| Connection string / ingestion key leakage | env var + Key Vault reference only; never logged; security-tagged tasks |
| Distro version churn (1.1.0 GA → 1.2 preview) | pin centrally; upgrade deliberately |

## 13. Locked decisions

| # | Decision | Locked value | Source |
|---|----------|--------------|--------|
| **D1** | Export backend | **Azure Monitor / Application Insights** via the **Microsoft OpenTelemetry Distro** (`Microsoft.OpenTelemetry` 1.1.0, `ExportTarget.AzureMonitor`). | User, 2025-10-15 |
| **D2** | Business-metric emission point | **Shared static instruments in `Weather.BuildingBlocks.Application`.** | User, 2025-10-15 |
| **D3** | EF Core instrumentation | **Skip** (no `db.client.*` metrics; EF logs remain). | User, 2025-10-15 |
| **D4** | Trace sampling | **100% dev, 10% prod** (`TracesPerSecond=null`, `SamplingRatio=1.0/0.1`). | User, 2025-10-15 |
| **D5** | Dashboards/alerts in scope | **Assume (a): commit Azure Monitor Workbook + alert-rule artifacts to the repo; deployment to Azure is a manual ops step, out of scope.** Not explicitly confirmed — flag if wrong. | Agent assumption |

## 14. Out of scope
- Authentication/authorisation, PII, and user-level analytics.
- Log retention policy and a second log-aggregation product.
- Continuous profiling, exemplars, tail-based sampling.
- EF Core / SQLite query-level instrumentation (D3).
- Replacing the existing `ILogger<T>` with a new logging facade.

## 15. References
- `specs/adr/0014-opentelemetry-observability.md`
- `specs/epics/e12-observability/`
- Microsoft Learn — [Enable OpenTelemetry for .NET](https://learn.microsoft.com/en-us/azure/azure-monitor/app/opentelemetry-enable)
- Microsoft OpenTelemetry Distro for .NET — [Azure Monitor getting started](https://github.com/microsoft/opentelemetry-distro-dotnet/blob/main/docs/azure-monitor-getting-started.md)
- OTel semantic conventions — [HTTP metrics](https://opentelemetry.io/docs/specs/semconv/http/http-metrics/)
