# Story e09s02 — Align Weather & Geography controllers to the Favorites response contract

| Field | Value |
|-------|-------|
| ID | e09s02 |
| Epic | e09 API Response Contract |
| Status | todo |
| BCPs | 2 |
| Risk | P2 |
| Type | refactor |

## 1. Summary
Apply the response-declaration pattern proven in `FavoritesController` to the Weather and Geography controllers:
declare a typed `[ProducesResponseType]` for every **reachable** outcome (success + each error), drop the
unreachable `400` from GET actions, keep success returning the resource directly, and lock the contract with an
OpenAPI test.

## 2. Context
Favorites makes outcomes unambiguous by declaring the full, accurate set of status codes per action and letting the
central `IExceptionHandler` produce the RFC 9457 Problem Details bodies. Weather/Geography still declare `400` on
GET actions where model binding cannot fail, so their contract is inaccurate (ADR-0011).

## 3. User story
As an API consumer, I want every action to declare its success and error outcomes, so I can tell success, failure,
and error apart and handle them programmatically.

## 4. Business value
A consistent, accurate, machine-readable contract across all endpoints; fewer surprises and easier troubleshooting.

## 5. In scope
- Weather `GET /api/weather/{cityName}`: declare `200` (`WeatherReportDto`), `404` (`ProblemDetails`),
  `422` (`ValidationProblemDetails`), `502` (`ProblemDetails`), `500` (`ProblemDetails`); **remove `400`**.
- Geography `GET /api/countries`: declare `200` (`IReadOnlyList<CountryDto>`), `500` (`ProblemDetails`);
  **remove `400`**.
- Geography `GET /api/countries/{countryCode}/cities`: declare `200` (`IReadOnlyList<CityDto>`), `404`,
  `422`, `500`; **remove `400`**.
- Keep success returning the resource directly (`Ok(resource)`).
- No POST actions ⇒ no `201`/`400`/`409` to port.
- An OpenAPI contract test asserting each action declares exactly the expected response codes.

## 6. Out of scope
Introducing API-layer request/response DTOs; changing module DTOs; changing status codes or error bodies; adding
auth (401/403) or rate limiting (429).

## 7. Domain concepts
None (presentation-only).

## 8. Layer touchpoints
| Assembly | Change |
|----------|--------|
| `Weather.Api` | `Modules/Weather/WeatherController`, `Modules/Geography/GeographyController` (annotations only) |
| `tests` | `Weather.IntegrationTests` — OpenAPI contract test |

No change to application/domain/infrastructure code or behaviour.

## 9. API contract
Unchanged bodies; only the declared response metadata changes:
- Weather: `200` `WeatherReportDto`; `404`/`502`/`500` `ProblemDetails`; `422` `ValidationProblemDetails`.
- Geography countries: `200` list; `500` `ProblemDetails`.
- Geography cities: `200` list; `404`/`500` `ProblemDetails`; `422` `ValidationProblemDetails`.

## 10. Dependencies
e09s01 (response contract) — done.

## 11. Risks
| Risk | Mitigation |
|------|-----------|
| Removing `400` hides a real binding failure | GET params are strings (route/query); no typed binding to fail. Confirmed by the OpenAPI contract test. |
| Drift between annotations and behaviour | Contract test asserts declared codes per action; existing ProblemDetails/Validation tests assert the bodies. |

## 12. Implementation steps
1. Remove the unreachable `400` `[ProducesResponseType]` from `WeatherController.GetCurrentWeather` → verify: `dotnet test src/Tests/Weather.IntegrationTests --filter Category=OpenApiContract`
2. Reduce `GeographyController.GetCountries` to `200` + `500` → verify: `… --filter Category=OpenApiContract`
3. Reduce `GeographyController.GetCitiesByCountry` to `200` + `404` + `422` + `500` → verify: `… --filter Category=OpenApiContract`
4. Add `OpenApiContractTests` asserting the per-action response codes in `/openapi/v1.json` → verify: `… --filter Category=OpenApiContract`
5. Standards gate → verify: `dotnet format src/Weather.Api/Weather.Api.csproj --verify-no-changes`

## 13. Test plan
- OpenAPI contract test: parse `/openapi/v1.json`; for each path+method assert the `responses` keys equal the expected set (e.g. Weather GET = {200,404,422,500,502}).
- Regression: existing `ProblemDetailsTests` / `ValidationTests` / `WeatherErrorsTests` / `GeographyErrorsTests` still green (bodies unchanged).

## 14. Acceptance criteria (Gherkin)
```gherkin
Scenario: Weather declares its outcomes
  Given the OpenAPI document
  When I inspect GET /api/weather/{cityName}
  Then its responses are exactly 200, 404, 422, 500, 502
  And no GET action declares 400

Scenario: Geography declares its outcomes
  When I inspect GET /api/countries
  Then its responses are exactly 200, 500
  When I inspect GET /api/countries/{countryCode}/cities
  Then its responses are exactly 200, 404, 422, 500

Scenario: Success stays a direct resource
  When I GET /api/countries
  Then the body is the array of countries (no envelope)
```

## 15. Definition of done
All action declarations match the reachable outcomes; contract test green; full preflight green.

## 16. Verification script
1. `curl -s localhost:5000/openapi/v1.json | jq '.paths["/api/weather/{cityName}"].get.responses | keys'`.

## 17. Out of scope
Request/response DTO reshaping; auth; rate limiting.

## 18. Risks / notes
Purely metadata; no behaviour or payload changes.

## 19. Sizing / BCP rationale
2 BCPs — three annotation edits + one contract test.

## 20. References
- `src/Weather.Api/Modules/Favorites/FavoritesController.cs` (reference pattern)
- `specs/adr/0011-rest-api-response-contract.md`, `docs/engineering/rest-api-standards.md`
