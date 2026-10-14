# Story e09s01 — API response contract (Problem Details + code + traceId + errors)

| Field | Value |
|-------|-------|
| ID | e09s01 |
| Epic | e09 API Response Contract |
| Status | todo |
| BCPs | 5 |
| Risk | P0 |
| Type | feat |

## 1. Summary
Implement the REST response contract centrally: every error is RFC 9457 Problem Details
(`application/problem+json`) carrying a stable `code` and a `traceId`, plus field-level `errors` for validation;
success returns the resource directly; creation returns `201` + `Location` + the created resource. Retrofits the
existing Weather and Geography endpoints.

## 2. Context
The current handler returns Problem Details without `code`/`traceId`/`errors`, and business validation returns
`400`. Callers cannot classify outcomes programmatically and support cannot correlate responses with logs
(ADR-0011).

## 3. User story
As an API consumer, I want an unambiguous, consistent response contract, so I can tell success, failure, and error
apart and act on them programmatically.

## 4. Business value
Predictable integration, faster troubleshooting, and a standards-based contract for every module.

## 5. In scope
- `ApiErrorCodes` + `ApiProblemTypes` constants (stable catalog; `https://weather.example.com/problems/...`).
- `AddProblemDetails` with `CustomizeProblemDetails` adding `traceId` (`Activity.Current?.Id` → `HttpContext.TraceIdentifier`).
- Central `IExceptionHandler` writing Problem Details via `IProblemDetailsService` with `code` (and `errors` for validation).
- FluentValidation → `422` `VALIDATION_FAILED` + `errors[]` (`field`, `code`, `message`).
- ModelState model-binding → `400` `MALFORMED_REQUEST` (`InvalidModelStateResponseFactory`).
- Mapping: not-found → `404 RESOURCE_NOT_FOUND`; conflict → `409 DUPLICATE_RESOURCE`; provider → `502 DEPENDENCY_UNAVAILABLE`; unhandled → `500 INTERNAL_ERROR`.
- `X-Correlation-ID` response header mirroring `traceId`.
- `[ProducesResponseType]` (success + error schemas) on Weather/Geography/Favorites actions.
- Tests asserting status, content type, and the presence/stability of `code` + `traceId` (+ `errors`).

## 6. Out of scope
Authentication/authorization (401/403); rate limiting (429); pagination metadata; async `202` (no async endpoints here).

## 7. Domain concepts
None (cross-cutting).

## 8. Layer touchpoints
| Assembly | Change |
|----------|--------|
| `Weather.Api` | `ErrorHandling/ApiErrorCodes.cs`, `ApiProblemTypes.cs`, rewritten `GlobalExceptionHandler`, `Program` (`AddProblemDetails` customization + `InvalidModelStateResponseFactory`), controllers (`[ProducesResponseType]`) |

## 9. API contract
- Errors: `application/problem+json` with `type`, `title`, `status`, `detail`, `instance`, `code`, `traceId` (+ `errors` for 422).
- Success: resource directly; create → `201` + `Location`.

## 10. Dependencies
Existing Weather/Geography controllers. Prerequisite for e04 (Favorites) and later modules.

## 11. Risks
| Risk | Mitigation |
|------|-----------|
| `422` change breaks existing tests | update the tests to the new contract |
| `traceId` absent in some paths | set it in the central customize hook, applied to every Problem Details |
| Duplicated formatting | one handler + one customize hook only |

## 12. Implementation steps
1. Add `ApiErrorCodes` + `ApiProblemTypes` → verify: `dotnet build src/Weather.Api/Weather.Api.csproj --warnaserror`
2. Configure `AddProblemDetails` with the `traceId` customization + `X-Correlation-ID` header → verify: `dotnet test src/Tests/Weather.IntegrationTests --filter Category=ProblemDetails`
3. Rewrite `GlobalExceptionHandler` to emit `code`/`errors` via `IProblemDetailsService` → verify: `… --filter Category=ProblemDetails`
4. FluentValidation → `422` + `VALIDATION_FAILED`; ModelState → `400` + `MALFORMED_REQUEST` → verify: `… --filter Category=Validation`
5. Retrofit Weather/Geography actions with `[ProducesResponseType]`; assert direct-resource success → verify: `dotnet test Weather.sln`
6. Assert error-code stability + `traceId` presence → verify: `… --filter Category=ProblemDetails`
7. Standards gate → verify: `dotnet format src/Weather.Api/Weather.Api.csproj --verify-no-changes`

## 13. Test plan
- Problem Details: `404`, `502`, `500` carry `code` + `traceId` + `application/problem+json`.
- Validation: `422` + `VALIDATION_FAILED` + `errors[]` with `field`/`code`/`message`.
- Model binding: `400` + `MALFORMED_REQUEST`.
- Success: `200` returns the resource directly; create returns `201` + `Location` + resource.

## 14. Acceptance criteria (Gherkin)
```gherkin
Scenario: Not found
  When I GET a missing resource
  Then the response is 404 with application/problem+json
  And the body has "code": "RESOURCE_NOT_FOUND" and a non-empty "traceId"

Scenario: Validation
  When I POST invalid business input
  Then the response is 422 with "code": "VALIDATION_FAILED"
  And the body lists the invalid fields under "errors"

Scenario: Success returns the resource directly
  When I GET /api/countries
  Then the response is 200 and the body is the array of countries (no envelope)
```

## 15. Definition of done
All existing endpoints conform; contract tests green; no error body without `code` + `traceId`.

## 16. Verification script
1. `curl -s -i localhost:5000/api/weather/Nowhere` → `404` + `application/problem+json` + `code` + `traceId`.

## 17. Out of scope
401/403/429; pagination; async 202.

## 18. Risks / notes
The 409/422 semantics also apply to the planned Favorites module (ADR-0010 F4).

## 19. Sizing / BCP rationale
5 BCPs — central handler + validation/model-binding wiring + retrofit + contract tests.

## 20. References
- `docs/engineering/rest-api-standards.md`
- `specs/adr/0011-rest-api-response-contract.md`, `specs/adr/0009-controller-based-apis.md`
