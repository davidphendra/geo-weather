# ADR-0004 — MediatR version and licensing posture

**Status:** Proposed (needs user confirmation)
**Date:** 2025-10-13

## Context
The requirement mandates CQRS using MediatR. MediatR v13 moved to a commercial license model
(free for individuals and small businesses; paid for larger organizations). v12.x remains Apache-2.0.

## Decision
Pin **MediatR 12.x** (latest 12.5.x) for this project.

## Rationale
- Avoids any licensing ambiguity for a public assessment repository.
- The API and behavior needed (commands, queries, `IPipelineBehavior`) are fully present in 12.x.
- Package stays permissively licensed (Apache-2.0).

## Consequences
- Positive: clear OSS licensing; no commercial terms in `NOTICE`.
- Negative: not the newest major version; migration to v13 is a future ADR if desired.
