# ADR-0012 — Single host: merge `Weather.Api` into `Weather.WebUI`

**Status:** Proposed (needs user confirmation)
**Supersedes (host split only):** parts of ADR-0005 and ADR-0009
**Date:** 2025-10-15

## Context
ADR-0005 sliced the repository into BuildingBlocks + four module assembly sets, a thin `Weather.Api` host,
and a Blazor `Weather.WebUI` *client* that was to consume "the API HTTP contract only". In practice the
WebUI was never wired to the API (it is still the default Blazor template) and the app has no external API
consumers. Running two ASP.NET Core processes for one small product is deployment and configuration overhead
with no compensating benefit: the REST surface and the UI have the same lifecycle, the same modules, and the
same data.

## Decision
Delete the `Weather.Api` project. `Weather.WebUI` becomes the **single composition root** for both surfaces:

1. API controllers, `ApiControllerBase`, the RFC 9457 error handling (`ApiErrorCodes`, `ApiProblemTypes`,
   `GlobalExceptionHandler`), and the API service wiring move into `Weather.WebUI` under an `Api/` folder with
   namespace `Weather.WebUI.Api.*`.
2. `Weather.WebUI` references each module's `Application` + `Infrastructure` and
   `Weather.BuildingBlocks.Infrastructure` — exactly the reference set `Weather.Api` had. It is the only
   project allowed to reference module implementations.
3. The REST API stays public and OpenAPI-documented; the Blazor UI consumes it over HTTP on the same origin.
4. The host stays **thin**: controllers bind → call one module's `I<Module>Service` → map the result. No
   application logic in controllers. Module boundaries and the async-only Events Bus rule are unchanged.
5. Exception handling is path-scoped: `/api/*` uses the Problem-Details `IExceptionHandler`; the Blazor `/Error`
   page handles the rest. `UseStatusCodePagesWithReExecute` is scoped off `/api` so API 404s stay JSON.

`specs/` terminology changes from "the API host" to "the host"; the module rules are unaffected.

## Consequences
- Positive: one process, one port, one `dotnet run`; no self-referential CORS; simpler deployment.
- Positive: the e09 REST contract, module isolation, and `scripts/check-layout.sh` are untouched.
- Positive: project count drops by one (`Weather.Api` removed); the API surface stays testable through
  `WebApplicationFactory<Program>` (the merged `Program`).
- Negative: `Weather.WebUI` is no longer "HTTP contract only" — it is the host. `CONVENTIONS.md`,
  `AGENTS.md`, and `tech-stack.md` must say so.
- Negative: two web concerns share one middleware pipeline; ordering must be explicit (see decision 5).
- Negative: the removal is one atomic move (a deleted project breaks every reference), not a series of
  micro-commits.

## Alternatives rejected
- **Keep two projects.** The stated status quo; rejected as overhead with no external API consumer.
- **Merge the UI into `Weather.Api` instead.** Equivalent mechanically, but `Weather.WebUI` is the surviving
  name chosen by the user.
- **Drop HTTP between Blazor and the API (in-process module calls).** Simplifies the runtime but invalidates
  the "consume the API contract" rule, the e06 `WeatherApiClient` stories, and the externally documented REST
  contract. Kept as an option; not chosen.

## Cross-references
- Story `e10s01`, epic `e10-single-host`.
- Amends ADR-0005 (repo slicing: no separate API host) and ADR-0009 (controllers now live in the WebUI host).
