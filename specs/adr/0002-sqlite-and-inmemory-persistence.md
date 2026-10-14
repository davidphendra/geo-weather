# ADR-0002 — SQLite for the app, EF Core InMemory for handler tests

**Status:** Proposed (needs user confirmation)
**Date:** 2025-10-13

## Context
The write side must persist records and demonstrate EF Core state mutations. The requirement
allows either the EF Core InMemory provider or SQLite.

## Decision
- **Running application:** EF Core with the **SQLite** provider (`weather.db`), schema via migrations.
- **Command-handler unit tests:** EF Core **InMemory** provider.
- **API integration tests:** SQLite **`:memory:`** with a held-open connection (real relational behavior).

## Rationale
- InMemory everywhere does not prove real persistence or relational constraints (unique indexes).
- SQLite gives file-backed durability and enforces unique constraints, matching "proper state persistence".
- InMemory keeps command-handler unit tests fast and dependency-free; a `DbContext` on InMemory can assert state.
- SQLite `:memory:` in integration tests catches constraint/query-translation issues InMemory hides.

## Consequences
- Positive: production-like persistence; constraints are real and tested.
- Negative: two providers to configure; EF configs must be provider-neutral.
- Negative: InMemory ignores some relational semantics — not used where those matter.

## Alternatives rejected
- **InMemory everywhere:** cannot demonstrate durable persistence or unique constraints.
