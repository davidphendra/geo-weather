# Story e15s03 — CSP origins for unpkg + OpenStreetMap tiles

| Field | Value |
|-------|-------|
| ID | e15s03 |
| Epic | e15 City Map on the Weather Page (M2, core) |
| Status | planned |
| BCPs | 2 |
| Risk | P0 |
| Type | feat |
| Delta | MODIFIED |

## 1. Summary
Allow the pinned Leaflet CDN origin and the OpenStreetMap tile host in the Content Security Policy, and
nothing else. Leaflet's script and stylesheet come from `https://unpkg.com`; its marker images are loaded
by the stylesheet from the same host; tiles come from `https://tile.openstreetmap.org` (ADR-0018 D10).

## 2. Context
`CspPolicy.Build` currently emits `script-src 'self' 'nonce-…'`, `style-src 'self' https://fonts.googleapis.com`,
and `img-src 'self' data:`. `CspOptions` already exposes configurable `StyleOrigins`/`FontOrigins` bound
from the `Csp` section. There is no script or image origin list yet. The Level 3 policy deliberately avoids
`'strict-dynamic'` and `'unsafe-inline'`.

## 3. User story
As a user, the map loads without a CSP violation while the rest of the policy stays locked down.

## 4. Business value
Keeps the map functional without weakening script or style policy beyond the single pinned CDN.

## 5. In scope
- `CspOptions.ScriptOrigins` (default `["https://unpkg.com"]`) and `CspOptions.ImageOrigins`
  (default `["https://unpkg.com", "https://tile.openstreetmap.org"]`).
- `CspPolicy` appends them to `script-src`/`script-src-elem`, `style-src`/`style-src-elem`
  (script host also serves the stylesheet), and `img-src`.
- `appsettings.json` needs no change (defaults live in code, matching `StyleOrigins`/`FontOrigins`).
- `CspPolicyTests` and `CspHeaderTests` updated.

## 6. Out of scope
Vendoring Leaflet; any `'unsafe-inline'`, `'unsafe-eval'`, or `'strict-dynamic'`; widening to `https:`;
the component and page wiring (e15s02/e15s04).

## 7. Domain concepts
None (WebUI security concern).

## 8. Layer touchpoints
| Assembly | Change |
|----------|--------|
| Weather.WebUI | `Security/CspOptions.cs`; `Security/CspPolicy.cs` |
| Weather.WebUI.UnitTests | `Security/CspPolicyTests.cs` |
| Weather.IntegrationTests | `Http/CspHeaderTests.cs` |

## 9. API contract (changed)
Every response's `Content-Security-Policy` header gains exactly:
- `script-src` / `script-src-elem`: `'self' 'nonce-…' https://unpkg.com`
- `style-src` / `style-src-elem`: `'self' https://fonts.googleapis.com https://unpkg.com`
- `img-src`: `'self' data: https://unpkg.com https://tile.openstreetmap.org`

## 10. Dependencies
e15s02 (the component that consumes these origins).

## 11. Risks
| Risk | Mitigation |
|------|-----------|
| Over-widening the policy | Only two explicit hosts added; a test asserts no `https:` wildcard, no `'unsafe-inline'`, no `'unsafe-eval'` |
| Marker images silently blocked | `img-src` includes the unpkg host, covered by a dedicated test |
| Report-only vs enforced divergence | `CspHeaderTests` runs with `Csp:EnforceInDevelopment=true` and asserts the enforced header |

## 12. Requirements (delta)
- **MODIFIED** — Content Security Policy origins.
  **Before:** `script-src 'self' 'nonce-N'`; `style-src 'self' https://fonts.googleapis.com`; `img-src 'self' data:`.
  **After:** the above plus `https://unpkg.com` in script and style, and `https://unpkg.com` +
  `https://tile.openstreetmap.org` in `img-src`.

## 13. Implementation steps
1. Add `ScriptOrigins` and `ImageOrigins` to `CspOptions` with the pinned defaults → verify: `dotnet test Weather.sln --filter Category=CspPolicy`
2. Thread them through `CspPolicy` (script/style/img) and update `CspPolicyTests` (new directives + a guard that nothing else widened) → verify: `dotnet test Weather.sln --filter Category=CspPolicy`
3. Update `CspHeaderTests` to assert the unpkg and tile origins on a rendered page → verify: `dotnet test Weather.sln --filter Category=CspHeaders`
4. Story gate: full preflight green → verify: `dotnet build Weather.sln --warnaserror && dotnet format Weather.sln --verify-no-changes && dotnet test Weather.sln`

## 14. Test plan
- Unit (`CspPolicy`): `script-src` and `script-src-elem` contain `https://unpkg.com`; `style-src`/`style-src-elem` contain both `https://fonts.googleapis.com` and `https://unpkg.com`; `img-src 'self' data: https://unpkg.com https://tile.openstreetmap.org`; still no `'unsafe-inline'`/`'unsafe-eval'`/`https:` wildcard; configured origins replace the defaults.
- Integration (`CspHeaders`): `GET /` carries the enforced header including the two new origins.

## 15. Acceptance criteria (Gherkin)
```gherkin
Scenario: Map assets are permitted
  Given the CSP header is enforced
  Then script-src allows https://unpkg.com
  And style-src allows https://unpkg.com
  And img-src allows https://unpkg.com and https://tile.openstreetmap.org

Scenario: The policy is not otherwise widened
  Given the CSP header is enforced
  Then it contains no 'unsafe-inline', 'unsafe-eval', or https: wildcard
```

## 16. Definition of done
Policy permits exactly the pinned CDN and tile host; tests assert the additions and the absence of broader
relaxations; Preflight green.

## 17. Verification script
1. `dotnet run --project src/Weather.WebUI`.
2. Open `/`, check the browser console: no CSP violations; the map tiles and marker render.

## 18. Risks / notes
The `script-src-elem`/`style-src-elem` directives mirror the base ones (the project already splits them for
the nonce route); no `connect-src` change is needed because Leaflet loads tiles as `<img>`.

## 19. Sizing / BCP rationale
2 BCPs — options + policy threading with test updates.

## 20. References
- `specs/adr/0018-city-map-leaflet.md` (D2, D3, D10)
- `src/Weather.WebUI/Security/CspPolicy.cs`
- `specs/tech-architecture/UI_DESIGN_PLAN_LATEST.md` (CSP decision D2/§)
