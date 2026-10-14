# Observability Runbook — Weather

> Signals are OpenTelemetry metrics, traces, and logs exported to Azure Monitor (Application Insights).
> Decision: `specs/adr/0014-opentelemetry-observability.md`. Metric catalog:
> `specs/tech-architecture/OBSERVABILITY_PLAN_LATEST.md`.

## 1. Signals at a glance

| Signal | Where | What it answers |
|--------|-------|-----------------|
| Metrics | Azure Monitor → Metrics / Workbook | Rate, errors, latency; runtime; business funnels |
| Traces | Application Insights → Transaction search | Which step failed/slowed |
| Logs | Application Insights → Logs (KQL) | Structured detail, correlated by `trace_id` |

Every RFC 9457 error response carries a `traceId` equal to the OpenTelemetry trace id and mirrored in the
`X-Correlation-ID` header. **To investigate a user report, search that trace id in Application Insights.**

## 2. Technical signals

| Question | Signal |
|----------|--------|
| Is the app up? | `requests` success rate; `GET /health/live`, `GET /health/ready` |
| Which routes are slow? | `http.server.request.duration` by route |
| Is the upstream weather provider healthy? | `weather.provider.requests`, `weather.provider.failures`, dependency table |
| Is the process under pressure? | `dotnet.gc.*`, `dotnet.thread_pool.*`, `dotnet.process.*`, Kestrel metrics |
| Are handlers slow/failing? | `mediatr.requests`, `mediatr.request.duration` |
| Are integration events flowing? | `integration.event.published/handled/failed` |

## 3. Business signals

| Question | Signal |
|----------|--------|
| How many weather lookups, and how do they end? | `weather.report.requests{weather.outcome}` |
| Are users finding cities? | `weather.city.not_found`, `geography.city.list_requests` |
| What is the save funnel? | `favorites.operations{favorites.outcome}`, `notes.operations{notes.outcome}` |
| How much has been saved? | `favorites.saved.count`, `notes.saved.count` |
| What is the error mix? | `api.errors{error.code}` |

Derived indicators (compute in the workbook, not instruments):

- Lookup → favorite conversion = `favorites.operations{operation=add,outcome=created}` ÷ `weather.report.requests{outcome=success}`.
- Catalogue coverage gap = `weather.city.not_found` ÷ `weather.report.requests`.
- Dependency error ratio = `weather.provider.failures` ÷ `weather.provider.requests`.

> **Validation failures** are recorded as `api.errors{error.code=VALIDATION_FAILED}` (FluentValidation rejects
> before the handler runs), so the handler funnels only show `created | duplicate | city_not_found`.

## 4. Common KQL queries

```kusto
// Error rate by stable code (last 24h)
customMetrics
| where timestamp > ago(24h) and name == 'api.errors'
| extend code = tostring(customDimensions['error.code'])
| summarize Errors = sum(valueSum) by code
| order by Errors desc

// Slowest routes (p95)
requests
| where timestamp > ago(1h)
| summarize p95 = percentile(duration, 95) by name
| order by p95 desc

// Trace a reported error
requests | where operation_Id == '<traceId from the error response>'

// Favorites funnel
customMetrics
| where timestamp > ago(24h) and name == 'favorites.operations'
| extend outcome = tostring(customDimensions['favorites.outcome'])
| summarize Operations = sum(valueSum) by outcome
```

## 5. Alerts and first response

| Alert | Meaning | First response |
|-------|---------|----------------|
| API error rate > 1% (`weather-availability`) | Availability SLO burning | Check `api.errors` by code; if `INTERNAL_ERROR`, open a trace; if `DEPENDENCY_UNAVAILABLE`, check the dependency panel |
| p95 latency > 1500 ms (`weather-latency`) | Weather endpoints slow | Check `weather.provider.duration`; if upstream is slow, confirm OpenWeatherMap status |
| OpenWeatherMap failures > 0 (`weather-dependency`) | Upstream failing | Check `weather.provider.failures{error.type}`: `timeout`/`unavailable`/`bad_status`/`empty_body`; verify the API key and provider status |
| No favorites created while traffic present (`weather-business-favorites`) | Business funnel stalled | Check `favorites.operations` outcomes (`duplicate`/`city_not_found` spikes); check validation failures |
| Integration event handler failed (`weather-integration-events`) | Projection may be stale | Find `integration.event.failed` in a trace; the failing handler is tagged `handler.module` |

## 6. Configuration and secrets

| Setting | Where | Notes |
|---------|-------|-------|
| `Observability:Enabled` | config | `false` by default (safe). Enable in a running environment with `Observability__Enabled=true`. |
| `Observability:ExportTarget` | config | `AzureMonitor` (prod) / `Console` (local) / `Otlp` |
| `Observability:SamplingRatio` | config | `0.1` prod, `1.0` dev. `TracesPerSecond` is set to `null` so the ratio applies. |
| `APPLICATIONINSIGHTS_CONNECTION_STRING` | App Service config / Key Vault | Never committed, never logged |

Local development: `dotnet run` uses the `launchSettings.json` profile which sets
`Observability__Enabled=true` and `Observability__ExportTarget=Console`.

## 7. Cardinality and PII policy (mandatory)

- **Never** attach a city name, note text, id, raw URL/path, query string, or exception message as a **metric**
  attribute. Only closed enums, country codes, HTTP methods, route templates, module/handler names, and status
  codes are allowed.
- High-cardinality detail belongs on **spans** and **logs**, never on metric dimensions.
- **Never** log secrets, tokens, connection strings, or API keys.
- Adding a new metric or attribute is a reviewed change: update the catalog in
  `specs/tech-architecture/OBSERVABILITY_PLAN_LATEST.md` first.

## 8. Dashboards and alerts as code

- Workbook: `ops/azure-monitor/workbook.json` (replace `{{AppInsightsResourceId}}`).
- Alert rules: `ops/azure-monitor/alerts.json` (ARM template; provide `appInsightsResourceId` and `actionGroupId`).
- Deploying these to Azure is a manual operations step, out of scope for the codebase.
