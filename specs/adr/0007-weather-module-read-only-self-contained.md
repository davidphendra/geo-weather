# ADR-0007 — The Weather module is read-only and self-contained

**Status:** Proposed (needs user confirmation)
**Date:** 2025-10-13

## Context
The Weather module (M2, core) is the first implementation epic. It has an unusual shape for a modular-monolith
module: it **persists nothing** (its data is an external provider), it has **no write side**, and it has **no
cross-module dependency** (the provider geocodes `q={city},{country}`). The planned BuildingBlocks
(`Entity`, `AggregateRoot`, `ValueObject`, `ICommand`, `IQuery`, `ValidationBehavior`) exist to serve the
write-side modules. Requiring them before the Weather module would make a shared library a prerequisite for a
module that uses none of its persistence/aggregate machinery.

## Decision
Build the Weather module **self-contained**, without BuildingBlocks:

1. **Domain value objects are `sealed record`s** with private constructors and factory methods (structural
   equality + guard clauses), not `ValueObject` subclasses.
2. **`WeatherReport` is an immutable `record`** with a `Create` factory — **not an `AggregateRoot`**, because
   there is no identity to persist and no consistency boundary to protect across transactions.
3. **CQRS uses MediatR directly** (`IRequest<T>` / `IRequestHandler<,>`) rather than a BuildingBlocks
   `IQuery<T>` marker. The module keeps its own `ValidationBehavior<TRequest,TResponse>` and its own
   `WeatherDomainException` / `WeatherProviderException` / `WeatherNotFoundException`.
4. **Packages** are added centrally (MediatR, FluentValidation, Microsoft.Extensions.Http/Options/Configuration);
   BuildingBlocks is not referenced for types.
5. When BuildingBlocks lands for the write modules, Weather **may** adopt `ValueObject` later — a low-churn
   refactor, not a prerequisite.

## Rationale
- Keeps the core domain free of a dependency it does not use.
- Lets the highest-value read feature ship before the shared library.
- Matches the modular-monolith rule that a module depends on what it needs; BuildingBlocks is an optimization,
  not a mandate.

## Consequences
- Positive: Weather builds and tests in isolation; no shared-library coupling.
- Positive: proves the module boundary is real — Weather genuinely needs nothing from the other modules.
- Negative: value-object equality/equality-components are hand-rolled via records instead of a shared base;
  if the codebase later standardizes on `ValueObject`, Weather is refactored then.
- Negative: the ValidationBehavior is duplicated from what BuildingBlocks will provide; the two are reconciled
  when BuildingBlocks lands.

## Locked refinements
Six design questions were resolved during planning; see `specs/epics/e03-weather/DECISIONS.md`:
clamp dew point to `min(derived, temperature)`; `decimal` throughout; `Location` is City + Country only;
nullable wind direction with cardinal `"Variable"`; API key via `DelegatingHandler` **and** a `Warning` log
level for the named client; `SkyCondition` as a typesafe value object.

## Alternatives rejected
- **Implement minimal BuildingBlocks first:** adds a shared library the Weather module does not use, contradicting
  the decision to start with the Weather module.
- **Make Weather persist a cache:** out of scope and would fabricate an aggregate for no requirement.
