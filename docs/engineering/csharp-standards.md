# C# Engineering Standards

Repository reference for all C# work. Mandatory rules first; recommendations and context-dependent
choices are marked. Concise day-to-day rules live in `AGENTS.md`; this file is the detailed reference.

Baseline: **.NET 10 / C# 14**, `Nullable` enabled, `TreatWarningsAsErrors`, `AnalysisLevel=latest`,
`EnforceCodeStyleInBuild=true` (see `Directory.Build.props`), `.editorconfig` for formatting/naming.

## 1. Standards hierarchy

When rules conflict, apply this order:

1. Correctness, security, data integrity.
2. Existing architecture and repository conventions.
3. Official .NET/C# guidelines (nullable, analyzers, formatting).
4. Maintainability and performance.

Never refactor working code only to impose a personal style. Follow repository conventions unless they
conflict with correctness, security, or an explicit standard.

## 2. Language and framework baseline

| Rule | Standard |
|------|----------|
| Target framework | `net10.0` |
| Language version | the framework default — **never** `<LangVersion>latest</LangVersion>` |
| Preview features | prohibited unless explicitly requested |
| Nullable reference types | enabled |
| Implicit usings | follow project config |
| Compiler warnings | fix meaningful warnings; never hide them |

Understand records, nullable reference types, pattern matching, collection expressions, primary
constructors, `init`/`required`, target-typed `new`, `nameof`, raw string literals, `Span<T>`,
`IAsyncEnumerable<T>`, and C# 14 extension members / null-conditional assignment. Use them when they
improve clarity, not for novelty.

## 3. Naming and formatting

| Element | Convention | Example |
|---------|------------|---------|
| Namespace | PascalCase | `Weather.Modules.Weather.Domain.WeatherReports` |
| Class / record / struct / enum | PascalCase | `WeatherReport` |
| Public method / property / constant | PascalCase | `FindAsync`, `CreatedAt` |
| Local / parameter | camelCase | `cityName` |
| Private instance field | `_camelCase` | `_httpClient` |
| Interface | `I` + PascalCase | `IWeatherService` |
| Generic parameter | `T` + name | `TResponse` |
| Async method | `Async` suffix | `GetCurrentWeatherAsync` |
| Boolean | predicate | `IsAvailable`, `CanReserve` |
| Enum member | PascalCase | `SkyCondition.Rain` |

- Four-space indent, Allman braces; braces on all control-flow bodies.
- One statement per line; one type per file; file-scoped namespaces.
- `using` directives ordered (System first), unused removed.
- `var` only when the type is obvious from the right-hand side.
- Prefer C# aliases (`string`, `int`, `bool`).
- No unexplained abbreviations; avoid long methods and deep nesting.
- Expression-bodied members only for simple expressions.

## 4. Type design, immutability, encapsulation

- **Class** — reference identity, mutable lifecycle, entities with behaviour.
- **Record / record class** — value-oriented data, messages, DTOs, immutable models.
- **Struct / record struct** — small value types with value semantics only.
- **Enum** — a finite, known set of states; no magic strings for internal state.
- **Interface** — only at meaningful boundaries (external deps, replaceable impls, test seams). Do not
  create an interface for every class.
- **Abstract class** — only when implementations genuinely share behaviour/invariants.
- **Static class** — stateless, no polymorphism, no injected dependencies.

Immutability:
- Prefer `readonly` fields and immutable value objects.
- Use `init` for initialize-only properties.
- Do not expose mutable collections; note that `IReadOnlyList<T>` is a contract, not deep immutability.
- Do not make domain entities immutable when the domain requires controlled transitions.
- Avoid `record` for ORM entities whose equality should be identity-based.

Encapsulation: private fields by default; expose behaviour, not internals; prefer explicit transition
methods over public setters; prefer composition; `seal` classes not designed for inheritance.

## 5. Nullability, validation, exceptions

- Enable and respect nullable reference types; model optional data explicitly.
- Never use the null-forgiving operator to hide unresolved nullability.
- Validate untrusted input at system boundaries; enforce invariants in the domain/application layer;
  do not duplicate the same validation in every layer.
- Prefer guard clauses and framework helpers: `ArgumentNullException.ThrowIfNull`,
  `ArgumentException.ThrowIfNullOrWhiteSpace`, `ArgumentOutOfRangeException.ThrowIfNegative`.
- Use specific exceptions that describe the failure; preserve inner exceptions when wrapping.
- Catch only when you can recover, translate meaningfully, or handle at a boundary.
- Never swallow exceptions or return misleading success values.
- Never expose stack traces, SQL, or secrets to clients; use `ProblemDetails`.
- Prefer a result type over exceptions for expected business outcomes when that is clearer.

## 6. Async and concurrency

- `async`/`await` for all I/O; never block with `.Result`/`.Wait()`.
- Propagate `CancellationToken` through the call chain.
- Do not wrap async I/O in `Task.Run`; no `async void` except event handlers.
- No unmanaged fire-and-forget; background work needs lifecycle, error handling, shutdown.
- Bound concurrency for large workloads (`Parallel.ForEachAsync`, channels).
- Never assume cancellation rolls back a transaction or external side effect.
- Do **not** blindly add `ConfigureAwait(false)` in ASP.NET Core (no synchronization context).

## 7. Collections and LINQ

- Choose collections by access/mutation pattern (`List<T>`, `IReadOnlyList<T>`, `Dictionary`, `HashSet`,
  `Queue`, `Stack`, `ConcurrentDictionary`, `ImmutableArray`, `Channel<T>`).
- Prefer clear LINQ for transforms/filters/projections; use loops when clearer.
- Respect deferred execution; do not enumerate expensive/stateful sequences repeatedly.
- `Any()` over `Count() > 0`; `FirstOrDefault`/`SingleOrDefault` by intent.
- Avoid needless `ToList()`/`ToArray()`. Know whether `IQueryable` runs in the database or in memory.

## 8. Dependency injection and resources

- Use the built-in container unless the project has a justified alternative.
- Lifetimes: singleton (stateless shared), scoped (`DbContext`, request services), transient (light).
- Never capture scoped dependencies in singletons without an explicit scope strategy.
- Keep `DbContext` short-lived; never share one across concurrent operations.
- Use `IHttpClientFactory` / registered clients; never `new HttpClient()` per request.
- Dispose what you own; `using` / `await using`.
- Avoid service-locator patterns, `BuildServiceProvider()` in registration, duplicate registrations.

## 9. Architecture, SOLID, separation of concerns

- Keep business rules out of controllers and infrastructure adapters; no DB access from presentation.
- Maintain module/project boundaries; no circular dependencies; no leaking persistence models into
  public contracts.
- Apply SOLID pragmatically. Introduce abstractions only when they improve testability, replaceability,
  or dependency direction.
- **Avoid** generic repositories over EF Core, unnecessary MediatR/factories/strategies, and layers
  without a demonstrated need.
- DDD patterns only where the domain justifies them.

## 10. Error handling, logging, observability

- Use `ILogger<T>` with structured properties (no interpolated strings).
- Appropriate levels; include identifiers/context; correlate with scopes/traces.
- **Never** log passwords, tokens, API keys, connection strings, or sensitive personal data.
- Do not log the same exception at every layer.
- Prefer metrics for aggregates, tracing for flows; avoid high-cardinality labels.
- External I/O: sensible timeouts, cancellation, retry only transient failures with bounded exponential
  backoff + jitter, idempotency guards; circuit breaker / rate limit by failure mode. Prefer
  `Microsoft.Extensions.Http.Resilience` and configure per dependency.

## 11. Database and EF Core

- Async APIs + cancellation tokens; parameterized queries only; no string-concatenated SQL.
- Project only needed columns; paginate large sets; `AsNoTracking()` for read-only queries.
- Avoid N+1 and unintended lazy loading.
- Transactions for atomic multi-step work; constraints/unique indexes in the database, not only in code.
- Reviewable, repeatable migrations; optimistic concurrency where updates can race.
- An existence check does not prevent concurrent duplicate inserts.

## 12. Security and API design

- Treat all cross-boundary input as untrusted (HTTP, tool output, model arguments, queue payloads).
- Authenticate/authorize on the server; least privilege; no hard-coded credentials.
- Parameterized SQL and safe serialization; no dangerous polymorphic deserialization.
- Request size limits, rate limits, timeouts; never leak internal errors.
- Authorization and validation are separate concerns; never trust client-supplied identity/roles.
- For AI agents: validate tool arguments against schemas; authorize every invocation independently;
  enforce resource-level access; treat retrieved content as untrusted; resist prompt injection; require
  confirmation for sensitive actions; never treat a model's "success" claim as proof.

## 13. Testing

- Add/update tests with meaningful behaviour changes; follow the repo's framework.
- Behaviours, not implementation details; Arrange–Act–Assert when it helps.
- Cover success, boundaries, invalid input, failure paths; test cancellation/concurrency/retries where
  material.
- Avoid excessive mocking and shared mutable state; use deterministic clocks/IDs/data.
- No test-order dependence; no arbitrary delays to synchronize.
- Use disposable/isolated databases for integration tests.
- Never weaken tests or production behaviour to get a green build.

## 14. Performance

- Clarity first; measure before optimizing (profiling, benchmarks, telemetry).
- Avoid needless allocations, round trips, repeated computation.
- Stream/bound large workloads; `Span<T>`/pooling only when justified.
- No unbounded caches/queues/background tasks; respect disposal.
- Never claim a performance improvement without evidence.

## 15. API contracts and serialization

- Explicit request/response DTOs at public boundaries; no internal types in contracts.
- Correct status codes; consistent error shape (`ProblemDetails`); never `200` for failure.
- Design pagination/filtering/sorting deliberately.
- Preserve naming/nullability/enum serialization unless a compatibility decision says otherwise.
- UTC or `DateTimeOffset` for instants; `DateOnly` for dates; `decimal` for exact/monetary values.

## 16. Modern C# usage

| Feature | Prefer when | Avoid when |
|---------|-------------|------------|
| Primary constructors | ctor logic simple and clear | validation/init is complex |
| Collection expressions | target type and intent clear | inference gets confusing |
| Records | value-based equality fits | identity semantics matter |
| Pattern matching | makes branching expressive | hides complex rules |
| Expression-bodied members | simple expressions | multi-step logic |
| C# 14 extension members | natural API over a type | surprising/ambiguous APIs |
| `field`-backed properties | a custom accessor needs a field | explicit storage is clearer |
| `Span<T>` | allocation-sensitive code | ordinary business code |
| `required` members | init must supply members | invariant belongs in ctor/factory |
| Source generators | repetitive compile-time value | a normal implementation is simpler |

## 17. Enforcement (this repository)

- `Directory.Build.props`: `Nullable`, `TreatWarningsAsErrors`, `AnalysisLevel=latest`,
  `EnforceCodeStyleInBuild=true`. Language version is the framework default — no `LangVersion=latest`.
- `.editorconfig`: formatting, naming (private `_camelCase`, `I` interfaces, `Async` suffix), style rules.
- `global.json`: pinned SDK.
- Preflight: `dotnet build --warnaserror && dotnet format --verify-no-changes && dotnet test`.

## 18. Mandatory workflow

Before: inspect code/config, understand architecture and conventions, identify behaviour/constraints/
failure modes, plan the smallest coherent change.
During: focused changes; preserve public contracts; correct error/cancellation/resource handling; tests;
no unrelated refactors or dependencies.
After: format/analyze, build affected projects, run relevant tests, review security/nullability/
concurrency/compatibility, and report commands + results honestly.

Never claim code builds, tests pass, or behaviour is verified unless the check was actually run.

## 19. Official references

- C# coding conventions — https://learn.microsoft.com/dotnet/csharp/fundamentals/coding-style/coding-conventions
- What's new in C# 14 — https://learn.microsoft.com/dotnet/csharp/whats-new/csharp-14
- .NET code analysis — https://learn.microsoft.com/dotnet/fundamentals/code-analysis/overview
- .NET runtime C# coding style — https://github.com/dotnet/runtime/blob/main/docs/coding-guidelines/coding-style.md
- ASP.NET Core HTTP resilience — https://learn.microsoft.com/dotnet/core/resilience/http-resilience
