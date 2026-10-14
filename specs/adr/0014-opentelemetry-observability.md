# ADR-0014 — OpenTelemetry observability with Azure Monitor

**Status:** Accepted (user decision 2025-10-15)
**Date:** 2025-10-15
**Relates to:** ADR-0011 (Problem Details `traceId`), ADR-0012 (single host), ADR-0013 (deployment)

## Context
The application ships to Azure App Service (Linux) with no telemetry beyond console `ILogger` output. An
operations team needs to monitor the service from both a technical and a business perspective, and the existing
RFC 9457 errors already carry a `traceId` that currently has no backing trace.

Requirements:
- One pipeline for metrics, traces, and logs (OpenTelemetry).
- Technical signals (RED, runtime, outbound dependency) and business signals (weather lookups, favorites, notes,
  integration events).
- No live network calls in automated tests.
- Respect the modular-monolith rules: modules never reference each other; Domain has no framework dependency.

## Decision
Adopt **OpenTelemetry** delivered through the **Microsoft OpenTelemetry Distro**
(`Microsoft.OpenTelemetry`, GA 1.1.0) exporting to **Azure Monitor / Application Insights**.

| # | Decision | Value |
|---|----------|-------|
| D1 | Export backend | Azure Monitor via `Microsoft.OpenTelemetry` (`ExportTarget.AzureMonitor`) |
| D2 | Business-metric emission point | Shared static instruments in `Weather.BuildingBlocks.Application` |
| D3 | EF Core instrumentation | Skip (no `db.client.*` metrics) |
| D4 | Trace sampling | 100% development, 10% production |
| D5 | Dashboards/alerts | Workbook + alert artifacts committed to the repo; deployment is a manual ops step |

Specifics:
1. The host (`Weather.WebUI`) is the only place that calls `builder.UseMicrosoftOpenTelemetry(...)`. The distro
   performs one-line onboarding for traces, metrics, and logs and bundles the ASP.NET Core and HttpClient
   instrumentation plus Azure resource detectors. The connection string is read from
   `APPLICATIONINSIGHTS_CONNECTION_STRING`; it is never committed or logged.
2. Custom signals are registered by name in the host:
   `ConfigureOpenTelemetryMeterProvider(b => b.AddMeter("Weather"))` and
   `ConfigureOpenTelemetryTracerProvider(b => b.AddSource("Weather.Activity"))`.
3. Instrument definitions live once in `Weather.BuildingBlocks.Application` (`WeatherMeters`, `WeatherActivity`).
   Modules emit inline; no module references an OpenTelemetry package. A `TelemetryBehavior` MediatR pipeline
   behavior (mirroring the existing `ValidationBehavior`) instruments CQRS requests.
4. Runtime metrics (`dotnet.*` GC / thread-pool) are **not** in the distro's built-in metric set, so
   `OpenTelemetry.Instrumentation.Runtime` is added explicitly.
5. Sampling: `TracesPerSecond` defaults to `5.0` and **takes precedence** over `SamplingRatio`. Production sets
   `TracesPerSecond = null` and `SamplingRatio = 0.1f`; development sets `SamplingRatio = 1.0f`. Metrics are never
   sampled.
6. `Observability:Enabled` gates the whole registration. Tests set it false, so no SDK is created and no socket
   can be opened. Metric/trace assertions use the BCL `MeterListener`/`ActivityListener`.
7. The `traceId` in Problem Details responses is the OpenTelemetry trace id
   (`Activity.Current?.Id`), so an error report maps to a trace.
8. Metric attribute cardinality is bounded: no city names, note text, ids, URLs, or exception messages. High
   cardinality lives on spans only.

## Evidence — what Microsoft documents
| Source | Statement used |
|--------|----------------|
| [Enable OpenTelemetry for .NET](https://learn.microsoft.com/en-us/azure/azure-monitor/app/opentelemetry-enable) | "New app or adopting OpenTelemetry for the first time: Use the Microsoft OpenTelemetry Distro." Sampling "is enabled by default at a rate of five requests per second". |
| [Microsoft OpenTelemetry Distro — Azure Monitor getting started](https://github.com/microsoft/opentelemetry-distro-dotnet/blob/main/docs/azure-monitor-getting-started.md) | `builder.UseMicrosoftOpenTelemetry(o => o.Exporters = ExportTarget.AzureMonitor);`, connection string auto-detected from `APPLICATIONINSIGHTS_CONNECTION_STRING`, built-in metrics limited to `Microsoft.AspNetCore.Hosting` and `System.Net.Http`, `AddMeter`/`AddSource` registration, and `TracesPerSecond` "takes precedence over `SamplingRatio` when set". |
| NuGet `Microsoft.OpenTelemetry` | GA `1.1.0`; targets `net8.0`/`netstandard2.0`. |

> Verified at planning time (2025-10-15) against the live NuGet feed and the Microsoft Learn / distro docs above.

## Consequences
- Positive: one package and one call deliver traces, metrics, and logs to Azure Monitor; no collector to run.
- Positive: business metrics are first-class (custom `Meter`), not derived second-hand.
- Positive: modules stay clean — no OpenTelemetry dependency crosses a module boundary (arch-test enforced).
- Positive: tests remain offline; no exporter can be constructed when telemetry is disabled.
- Negative: Azure Monitor couples the stack to one vendor; the distro also supports `ExportTarget.Otlp`, so
  switching later is a host-only change (no module edits).
- Negative: no query-level database visibility while EF Core instrumentation is skipped (D3). Revisit if query
  latency becomes a production question.
- Negative: ingestion volume costs money — mitigated by aggregation, trace sampling, and rate-aware defaults.
- Negative: distro is a relatively young GA package (1.x); it is pinned centrally and upgraded deliberately.

## Alternatives
- **`Azure.Monitor.OpenTelemetry.AspNetCore`** (legacy distro, `AddOpenTelemetry().UseAzureMonitor()`). Rejected:
  Microsoft now directs new adoptions to `Microsoft.OpenTelemetry`.
- **Raw OpenTelemetry SDK + OTLP exporter.** Backend-neutral but adds several packages and a collector to operate.
  Rejected for D1=a; a future switch is host-local.
- **OTLP to a self-hosted collector (Grafana/Alloy).** Rejected by D1; still reachable via `ExportTarget.Otlp`.
- **Prometheus scrape endpoint.** No scrape infrastructure exists; rejected.
- **Host-only business metrics** (emit in controllers). Rejected: puts application logic in the thin host and
  cannot see provider/handler-level outcomes.
- **Derive business metrics from spans in the collector.** Rejected: less explicit, more moving parts, and would
  still need a collector (not chosen).
- **EF Core instrumentation (beta).** Rejected by D3.
