# ADR-0011 — REST API response contract

**Status:** Proposed (needs user confirmation)
**Date:** 2025-10-13
**Reference:** `docs/engineering/rest-api-standards.md`

## Context
The existing controllers return `ActionResult<T>` with the resource body on success, and `GlobalExceptionHandler`
returns Problem Details on failure — but the error body has no stable `code`, no `traceId`, and no field-level
`errors`, and validation returns `400` rather than `422`. Callers cannot reliably classify an outcome
programmatically, and support cannot correlate a response with server logs.

## Decision
Adopt the REST API response contract in `docs/engineering/rest-api-standards.md` across all modules:

1. **Status code is the primary outcome.** No `200` for failures.
2. **Success = the resource directly** (no `success: true` envelope). Creation returns `201` + `Location` +
   the created resource; delete returns `204`.
3. **Errors = RFC 9457 Problem Details** (`application/problem+json`) with `type`, `title`, `status`, `detail`,
   `instance`, plus extensions **`code`** (stable, machine-readable) and **`traceId`**.
4. **Validation**: FluentValidation → `422` + `VALIDATION_FAILED` + `errors[]` (`field`, `code`, `message`);
   model binding/`ModelState` → `400` + `MALFORMED_REQUEST`.
5. **Stable error-code catalog**: `MALFORMED_REQUEST`, `VALIDATION_FAILED`, `RESOURCE_NOT_FOUND`,
   `DUPLICATE_RESOURCE`, `DEPENDENCY_UNAVAILABLE`, `INTERNAL_ERROR`.
6. **`traceId`** from `Activity.Current?.Id`, falling back to `HttpContext.TraceIdentifier`; added centrally via
   `AddProblemDetails(options => options.CustomizeProblemDetails = …)`; mirrored in an `X-Correlation-ID` header.
7. **Never leak internals**; log the exception once and correlate by `traceId`.
8. `[ProducesResponseType]` on every action (success + error schemas); OpenAPI documents the contract.

## Consequences
- Positive: unambiguous, standards-based contract; callers classify via status + `code`; support uses `traceId`.
- Positive: one central implementation (`AddProblemDetails` customization + `IExceptionHandler` + error-code
  constants); endpoints stay thin.
- Negative: a small retrofit of the already-built Weather and Geography endpoints and the exception handler.
- Negative: `409` and `422` change some existing behaviours (Favorites duplicates → `409`, validation → `422`).

## Alternatives rejected
- **`{ success: boolean }` envelope:** redundant with HTTP status; rejected by the reference.
- **Free-form error messages only:** not machine-readable; rejected.
- **Leaving `400` for business validation:** conflates malformed requests with business-rule failures; rejected
  (`422` for the latter).

## Cross-references
- Applied to current + future modules via epic `e09-api-response-contract` (retrofits Weather/Geography; the
  contract Favorites and later modules build to).
- Relates to ADR-0009 (controller-based APIs).
