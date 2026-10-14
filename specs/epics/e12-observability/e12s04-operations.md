# Story e12s04 — Operations: dashboards, alerts, runbook, docs

| Field | Value |
|-------|-------|
| ID | e12s04 |
| Epic | e12 Observability |
| Status | todo |
| BCPs | 5 |
| Risk | P1 |
| Type | feat |
| ADR | ADR-0014 |

## 1. Summary
Give the operations team the artifacts it consumes: an Azure Monitor Workbook (technical + business panels),
alert-rule templates, and a runbook, plus the documentation and epic registration that keep the repository the
source of truth.

## 2. Context
Metrics and traces exist after e12s01–e12s03 but nothing tells an operator where to look or what "bad" is.
ADR-0014 D5 commits the dashboard/alert artifacts to the repository; deploying them to Azure is a manual ops
step, out of scope.

## 3. User story
As an operator, I want a dashboard, alerts, and a runbook, so I can detect and diagnose issues without knowing
the codebase.

## 4. Business value
Converts telemetry into an operational capability; satisfies the production-readiness gate for the App Service
deployment.

## 5. In scope
- `ops/azure-monitor/workbook.json` — Azure Monitor Workbook with panels: availability/latency/errors,
  dependency health, runtime, business funnels, saved-count gauges.
- `ops/azure-monitor/alerts.json` — ARM alert rules for the SLOs in the plan §10.
- `docs/operations/observability.md` — runbook: signals, queries (KQL), alert runbooks, trace drill-down,
  cardinality/PII policy, validation-failure→`api.errors` mapping.
- Updates: `AGENTS.md` (Observability commands table), `CONVENTIONS.md` (cardinality/no-PII/telemetry rules),
  `specs/tech-architecture/tech-stack.md`, `TEST_PLAN_LATEST.md`.
- Register e12 in `release-plan.yaml` + `execution-status.yaml`; mark the plan implemented.

## 6. Out of scope
- Deploying the workbook/alerts to Azure (manual ops step).
- Log retention and cost policy.
- EF Core instrumentation.

## 7. Domain concepts
None.

## 8. Layer touchpoints
No code assemblies change.

## 9. API contract
Unchanged.

## 10. Dependencies
e12s01, e12s02, e12s03.

## 11. Risks
| Risk | Mitigation |
|------|-----------|
| Workbook JSON drifts from real metric names | generate queries from the plan catalog; a doc test greps metric names |
| Alerts too noisy | thresholds from the plan §10 with sustained-window conditions |
| Runbook describes signals that don't exist | every panel references a metric asserted in e12s02/e12s03 tests |
| Secret in artifacts | connection string referenced by name only; `security-review` on changed paths |

## 12. Implementation steps
1. Add `ops/azure-monitor/workbook.json` (technical + business panels) → verify: `python3 -c "import json;json.load(open('ops/azure-monitor/workbook.json'))"`
2. Add `ops/azure-monitor/alerts.json` (SLO alert rules) → verify: `python3 -c "import json;json.load(open('ops/azure-monitor/alerts.json'))"`
3. Add `docs/operations/observability.md` runbook → verify: `test -f docs/operations/observability.md`
4. Update `AGENTS.md` + `CONVENTIONS.md` (Observability section, cardinality rule) → verify: `grep -q "Observability" AGENTS.md && grep -q "cardinality" CONVENTIONS.md`
5. Update `tech-stack.md` + `TEST_PLAN_LATEST.md` + `OBSERVABILITY_PLAN_LATEST.md` (status implemented) → verify: `grep -q "Microsoft.OpenTelemetry" specs/tech-architecture/tech-stack.md`
6. Cross-check every metric name referenced by the workbook/alerts against the code/plan → verify: `grep -oE "(weather|favorites|notes|integration|api|mediatr|geography|http|dotnet)\\.[a-z_.]+" ops/azure-monitor/workbook.json | sort -u`
7. Register e12 in `release-plan.yaml` + `execution-status.yaml`; ADR-0014 + capsule present → verify: `test -f specs/adr/0014-opentelemetry-observability.md && grep -q "e12-observability" specs/release-plan.yaml && grep -q "e12s01" specs/execution-status.yaml`
8. Full preflight → verify: `dotnet build Weather.sln --warnaserror && dotnet format Weather.sln --verify-no-changes && dotnet test Weather.sln`

## 13. Test plan
| Scenario | Level | Risk |
|----------|-------|------|
| `SC-e12s04-P1-01` Workbook and alert artifacts are valid JSON | Doc/CI | P1 |
| `SC-e12s04-P1-02` Every metric named in the artifacts exists in the plan catalog | Doc/CI | P1 |
| `SC-e12s04-P1-03` Observability commands documented in `AGENTS.md` | Doc | P1 |

## 14. Acceptance criteria (Gherkin)
```gherkin
Scenario: Operations artifacts exist
  Then ops/azure-monitor/workbook.json and alerts.json are valid JSON
  And docs/operations/observability.md describes signals and alert runbooks

Scenario: Documented signals are real
  Given the workbook references metric names
  Then every name appears in the observability plan catalog

Scenario: Contributor guidance
  Then AGENTS.md documents how to view telemetry
  And CONVENTIONS.md states the metric cardinality and no-PII rules
```

## 15. Definition of done
Artifacts valid; runbook complete; docs updated; epic registered; full preflight green.

## 16. Verification script
1. `python3 -c "import json;json.load(open('ops/azure-monitor/workbook.json'));json.load(open('ops/azure-monitor/alerts.json'))"`
2. `test -f docs/operations/observability.md`
3. `grep -q "Observability" AGENTS.md`
4. Full preflight.

## 17. Out of scope
Section 6.

## 18. Risks / notes
The workbook is a template requiring the App Insights resource id and subscription at deployment time; document
those placeholders in the runbook.

## 19. Sizing / BCP rationale
5 BCPs — workbook, alerts, runbook, four doc updates, registration + preflight.

## 20. References
- `specs/tech-architecture/OBSERVABILITY_PLAN_LATEST.md` §§7, 10, 13
- `specs/adr/0014-opentelemetry-observability.md`
