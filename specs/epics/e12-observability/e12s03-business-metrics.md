# Story e12s03 — Business metrics (weather, geography, favorites, notes, errors)

| Field | Value |
|-------|-------|
| ID | e12s03 |
| Epic | e12 Observability |
| Status | todo |
| BCPs | 6 |
| Risk | P0 |
| Type | feat |
| ADR | ADR-0014 |

## 1. Summary
Emit the business signals the operations team uses: weather lookup outcomes, geography catalogue usage,
favorites/notes funnels with saved-count gauges, and the stable `api.errors` error taxonomy.

## 2. Context
e12s01/e12s02 deliver the pipeline and technical instruments. This story adds the domain-meaningful counters
that answer product questions (lookup volume, conversion to favorites, note creation, failure mix) using the
shared instruments from `Weather.BuildingBlocks.Application` (ADR-0014 D2).

## 3. User story
As an operator/product owner, I want business counters for the core flows, so I can see whether the product is
being used and where it fails.

## 4. Business value
Turns raw HTTP traffic into product KPIs (lookup success rate, save funnel, dependency health, error mix).

## 5. In scope
- Weather: `weather.report.requests` / `.duration` (outcome success|not_found|provider_error|validation_error,
  `geo.country_code`), `weather.city.not_found`.
- Geography: `geography.country.list_requests` (`geo.has_search`), `geography.city.list_requests`
  (`geo.country_code`).
- Favorites: `favorites.operations` (add: created|duplicate|city_not_found; list: success), `favorites.duration`,
  `favorites.saved.count` ObservableGauge (from `FavoritesContext`).
- Notes: `notes.operations` (created|city_not_found), `notes.duration`, `notes.saved.count` ObservableGauge.
- `api.errors{error.code, http.response.status_code, http.request.method, http.route}` in
  `GlobalExceptionHandler` and the malformed-request path.
- Validation failures surface as `api.errors{error.code=VALIDATION_FAILED}` (the pipeline rejects before the
  handler; the handler-level funnels therefore exclude `invalid`).

## 6. Out of scope
- New endpoints or behaviour changes.
- Dashboards/alerts (e12s04).
- User-level analytics and PII.

## 7. Domain concepts
Outcome and funnel semantics only; no domain model change.

## 8. Layer touchpoints
| Assembly | Change |
|----------|--------|
| `Weather.Modules.Weather.Application` | handler metrics |
| `Weather.Modules.Geography.Application` | query-handler metrics |
| `Weather.Modules.Favorites.Application` | handler metrics |
| `Weather.Modules.Favorites.Infrastructure` | `favorites.saved.count` gauge |
| `Weather.Modules.Notes.Application` | handler metrics |
| `Weather.Modules.Notes.Infrastructure` | `notes.saved.count` gauge |
| `Weather.WebUI` | `api.errors` in `GlobalExceptionHandler` + malformed-request customization |

## 9. API contract
Unchanged.

## 10. Dependencies
e12s01, e12s02.

## 11. Risks
| Risk | Mitigation |
|------|-----------|
| High-cardinality attributes sneak in | only `geo.country_code`, closed outcome/operation enums; review every `.Add` |
| Gauges query the DB on every scrape | gauges read a single `COUNT(*)` with `AsNoTracking()`; acceptable at this scale |
| Gauge callback throws and breaks export | callback is defensive; a failure increments an internal error and returns last-known value |
| Counters double-count on retries | emit once per completed operation, after the outcome is known |
| Error taxonomy drifts from ADR-0011 | reuse `ApiErrorCodes` constants verbatim |

## 12. Implementation steps
1. Weather business metrics in the handler (and not-found counter) → verify: `dotnet test src/Modules/Weather/Tests/Weather.Modules.Weather.Tests --filter Category=Observability`
2. Geography business metrics in the query handlers → verify: `dotnet test src/Modules/Geography/Tests/Weather.Modules.Geography.Tests --filter Category=Observability`
3. Favorites metrics + `favorites.saved.count` gauge → verify: `dotnet test src/Modules/Favorites/Tests/Weather.Modules.Favorites.Tests --filter Category=Observability`
4. Notes metrics + `notes.saved.count` gauge → verify: `dotnet test src/Modules/Notes/Tests/Weather.Modules.Notes.Tests --filter Category=Observability`
5. `api.errors` in `GlobalExceptionHandler` + malformed-request path → verify: `dotnet test src/Tests/Weather.IntegrationTests --filter Category=Observability`
6. Business-path assertions (success / not_found / duplicate / validation) via `MeterListener` → verify: `dotnet test Weather.sln --filter Category=Observability`
7. Full preflight → verify: `dotnet build Weather.sln --warnaserror && dotnet format Weather.sln --verify-no-changes && dotnet test Weather.sln`

## 13. Test plan
| Scenario | Level | Risk |
|----------|-------|------|
| `SC-e12s03-P0-01` A successful weather lookup increments `weather.report.requests{outcome=success}` | Integration | P0 |
| `SC-e12s03-P0-02` Unknown city increments `outcome=not_found` + `weather.city.not_found` | Integration | P0 |
| `SC-e12s03-P0-03` Provider failure increments `outcome=provider_error` | Integration | P0 |
| `SC-e12s03-P0-04` Favorite created/duplicate/city_not_found increment the matching outcome | Unit | P0 |
| `SC-e12s03-P0-05` Validation failure increments `api.errors{code=VALIDATION_FAILED}` | Integration | P0 |
| `SC-e12s03-P1-01` `favorites.saved.count` / `notes.saved.count` reflect row counts | Integration | P1 |
| `SC-e12s03-P1-02` Geography list requests carry country code / has_search | Unit | P1 |

## 14. Acceptance criteria (Gherkin)
```gherkin
Scenario: Weather lookup outcomes
  Given telemetry is enabled
  When I GET /api/weather/London successfully
  Then weather.report.requests{outcome=success} increases
  When I GET /api/weather/Nowhere and it is unknown
  Then weather.report.requests{outcome=not_found} and weather.city.not_found increase

Scenario: Save funnels
  When a new favorite is created
  Then favorites.operations{operation=add,outcome=created} increases
  When the same favorite is added again
  Then favorites.operations{operation=add,outcome=duplicate} increases

Scenario: Error taxonomy
  When a request fails validation
  Then api.errors{code=VALIDATION_FAILED} increases

Scenario: Saved-count gauges
  When the favorites database has N rows
  Then favorites.saved.count reports N
```

## 15. Definition of done
Build, format, tests green; each business path asserted; no high-cardinality attribute present; plan catalog and
implementation agree on names/units/attributes.

## 16. Verification script
1. `dotnet test Weather.sln --filter Category=Observability`
2. `dotnet test src/Modules/Favorites/Tests/Weather.Modules.Favorites.Tests`
3. `dotnet test src/Modules/Notes/Tests/Weather.Modules.Notes.Tests`
4. Full preflight.

## 17. Out of scope
Section 6.

## 18. Risks / notes
The `invalid` outcome in the plan catalog is observed as `api.errors{code=VALIDATION_FAILED}` rather than a
handler funnel value, because FluentValidation rejects before the handler runs. Keep the plan catalog's allowed
values but document this mapping in the runbook (e12s04).

## 19. Sizing / BCP rationale
6 BCPs — weather, geography, favorites, notes, gauges, error taxonomy, path assertions.

## 20. References
- `specs/tech-architecture/OBSERVABILITY_PLAN_LATEST.md` §5.4
- `specs/adr/0014-opentelemetry-observability.md`
