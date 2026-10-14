# REST API JSON Response Standards

Repository reference for every HTTP endpoint. Concise rules live in `AGENTS.md`; this is the detailed reference.

Goal: a caller can always tell **whether a request succeeded**, **why it failed**, **how to troubleshoot**, and
**what to do next** — from the HTTP status code, a consistent success body, and an RFC 9457 Problem Details error body.

## 1. The HTTP status code is the primary outcome signal

| Code | Meaning | Use |
|------|---------|-----|
| `200 OK` | Success | Resource retrieved / operation completed |
| `201 Created` | Resource created | New resource; include a `Location` header and the created resource |
| `202 Accepted` | Accepted for async processing | Job queued, not yet complete |
| `204 No Content` | Success, no body | Delete |
| `400 Bad Request` | Malformed request | Malformed JSON / model-binding failure |
| `401`/`403` | Auth | (no auth in scope) |
| `404 Not Found` | Resource missing | Unknown id/city/country |
| `409 Conflict` | State conflict | Duplicate resource |
| `422 Unprocessable Content` | Validation failure | Valid JSON, invalid business input |
| `429 Too Many Requests` | Rate limit | Respect `Retry-After` |
| `500 Internal Server Error` | Unexpected server error | Never leak details |
| `502 Bad Gateway` | Invalid upstream response | Provider returned bad data |
| `503 Service Unavailable` | Temporarily unavailable | May include `Retry-After` |
| `504 Gateway Timeout` | Upstream timeout | |

Rules:
- NEVER return `200` for a failed operation.
- Use `202` only for genuinely asynchronous work; `204` carries no body.
- Be consistent across every endpoint.

## 2. Errors: RFC 9457 Problem Details (`application/problem+json`)

Every error response is Problem Details with the standard fields plus `code`, `traceId`, and (for validation) `errors`.

```json
{
  "type": "https://weather.example.com/problems/resource-not-found",
  "title": "Resource not found",
  "status": 404,
  "detail": "City 'Nowhere (ZZ)' was not found.",
  "instance": "/api/cities/favorites",
  "code": "RESOURCE_NOT_FOUND",
  "traceId": "00-4bf92f3577b34da6a3ce929d0e0e4736-00f067aa0ba902b7-01"
}
```

| Field | Purpose |
|-------|---------|
| `type` | Stable URI for the problem category |
| `title` | Short human-readable summary |
| `status` | HTTP status repeated in the body |
| `detail` | Explanation of this specific occurrence |
| `instance` | The request path/resource involved |
| `code` | Stable, machine-readable application error code |
| `traceId` | Correlates the response with logs/traces |
| `errors` | Field-level validation errors when applicable |

## 3. Success: return the resource directly

Use direct resource responses (no `success: true` envelope) for CRUD.

- `GET /api/countries` → `200` `[ { "code": "GB", "name": "United Kingdom" } ]`
- `GET /api/cities/favorites` → `200` `[ { "id", "cityId", "city", "countryCode", "createdAtUtc" } ]`
- `POST /api/cities/favorites` → `201` + `Location: /api/cities/favorites/{id}` + the created resource

An envelope is allowed only when there is a genuine, consistent need for metadata (e.g. pagination). Never introduce a
wrapper solely to add `"success": true`.

## 4. Stable, machine-readable error codes

| `code` | Meaning | Typical caller action |
|--------|---------|-----------------------|
| `MALFORMED_REQUEST` | Bad JSON / unbound parameters (400) | Fix the request |
| `VALIDATION_FAILED` | Business validation failed (422) | Correct the listed fields |
| `RESOURCE_NOT_FOUND` | Resource does not exist (404) | Check the identifier |
| `DUPLICATE_RESOURCE` | Already exists (409) | Retrieve/update the existing resource |
| `DEPENDENCY_UNAVAILABLE` | Upstream provider failed (502) | Retry if safe |
| `INTERNAL_ERROR` | Unexpected failure (500) | Report the `traceId` |

- Keep codes stable across versions; use the documented set, not ad-hoc strings.
- Callers MUST NOT depend on the wording of `detail`.
- Distinguish validation, business-rule, and infrastructure failures.

## 5. Field-level validation errors

Return every actionable error in one response.

```json
{
  "type": "https://weather.example.com/problems/validation-error",
  "title": "Request validation failed",
  "status": 422,
  "detail": "One or more fields are invalid.",
  "instance": "/api/cities/favorites",
  "code": "VALIDATION_FAILED",
  "traceId": "00-…",
  "errors": [
    { "field": "cityName", "code": "NOT_EMPTY", "message": "City name is required." },
    { "field": "countryCode", "code": "LENGTH", "message": "Country code must be 2 characters." }
  ]
}
```

- Use a consistent field path (`customer.email`, `items[0].quantity`).
- Stable validation codes; specific messages; never echo secrets.

## 6. Correlation / trace ID

- Prefer the active `Activity.Current?.Id`; fall back to `HttpContext.TraceIdentifier`.
- Return it in the Problem Details body and propagate it into structured logs.
- Optionally mirror it in an `X-Correlation-ID` response header.
- NEVER treat a caller-supplied correlation id as identity/authorization.

## 7. Retry semantics

| Status | Caller behaviour |
|--------|------------------|
| 400/422 | Do not retry without fixing |
| 404 | Usually do not retry |
| 409 | Resolve the conflict first |
| 429/503 | Respect `Retry-After` |
| 502/504 | Bounded retries with backoff, only if safe |
| Timeout | Retry only if idempotent |

Creation endpoints should be idempotent-safe (unique constraints; optionally an idempotency key).

## 8. Never leak internals

Return only: stable `code`, validation `errors`, public ids, `traceId`, sanitized `detail`.
NEVER return: stack traces, raw exception messages, SQL/connection strings, hostnames, tokens/keys, file paths,
or unfiltered downstream payloads. Log the detail server-side and correlate via `traceId`.

## 9. Implementation (.NET 10 / ASP.NET Core, controller-based)

- `builder.Services.AddProblemDetails(options => options.CustomizeProblemDetails = ctx => ctx.ProblemDetails.Extensions.TryAdd("traceId", Activity.Current?.Id ?? ctx.HttpContext.TraceIdentifier));`
- A central `IExceptionHandler` maps exceptions → Problem Details (`type`, `title`, `status`, `detail`, `code`, `errors`), writes via `IProblemDetailsService`, and logs the exception once.
- FluentValidation failures → `422` + `VALIDATION_FAILED` + `errors[]` (mapped from `ValidationFailure`).
- `[ApiController]` model-binding/ModelState failures → `400` + `MALFORMED_REQUEST` (via `ApiBehaviorOptions.InvalidModelStateResponseFactory`).
- Exception → code map: `NotFoundException`→404 `RESOURCE_NOT_FOUND`; `ConflictException`→409 `DUPLICATE_RESOURCE`; validation→422 `VALIDATION_FAILED`; provider failure→502 `DEPENDENCY_UNAVAILABLE`; unhandled→500 `INTERNAL_ERROR`.
- `[ProducesResponseType]` on every action, including error schemas; document with OpenAPI.
- Automated tests assert status code, `application/problem+json`, required fields (`code`, `traceId`), and error-code stability.

## 10. Predictable contract (summary)

| Concern | Convention |
|---------|-----------|
| Retrieval | `200` + resource/collection |
| Creation | `201` + `Location` + created resource |
| Async | `202` + operation reference |
| Delete | `204`, no body |
| Client error | `4xx` + Problem Details |
| Server error | `5xx` + sanitized Problem Details |
| Validation | `422` + field-level `errors` |
| Troubleshooting | `traceId` |
| Classification | stable `code` |

> `200` means the HTTP request was handled; it does not mean a long-running business process completed. Use `202`
> for asynchronous work and expose status separately.

## References
- RFC 9457 — Problem Details for HTTP APIs
- ASP.NET Core error handling & Problem Details — https://learn.microsoft.com/aspnet/core/fundamentals/error-handling
- API conventions / OpenAPI in ASP.NET Core — https://learn.microsoft.com/aspnet/core/web-api/
