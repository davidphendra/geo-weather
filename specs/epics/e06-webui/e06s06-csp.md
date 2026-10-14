# Story e06s06 — Content Security Policy Level 3

| Field | Value |
|-------|-------|
| ID | e06s06 |
| Epic | e06 WebUI Front-End |
| Status | todo |
| BCPs | 5 |
| Risk | P1 |
| Type | feat |

## 1. Summary
Apply a CSP Level 3 policy to **every** response via a nonce-based `Content-Security-Policy` header, including the
Blazor bootstrap script and `ImportMap`, plus a violation report sink at `/_csp/report`.

## 2. Context
The UI is a Blazor Web App (`InteractiveServer`). The `ImportMap` component renders an inline script, and Blazor
Web Apps already emit `frame-ancestors 'self'`; both need explicit handling. A header (not a `<meta>` tag) is
required for `frame-ancestors`, `report-to`, and `sandbox` (Microsoft Learn).

## 3. User story
As a security owner, I want a strict CSP enforced on all pages, so XSS/clickjacking surface is reduced without
breaking Blazor interactivity.

## 4. Business value
Hardens the app's only public surface; a documented, tested security control.

## 5. In scope
- `CspMiddleware` generating a per-request nonce (32-byte, Base64) stored in `HttpContext.Items`, emitting the
  header on all responses (+ `Reporting-Endpoints`), `Cache-Control: no-store` for nonce-bearing HTML.
- `CspPolicy` directive builder; `CspOptions` (`EnforceInDevelopment`, `ReportPath`).
- Nonce applied to `ImportMap` and `blazor.web.js` in `App.razor` via cascading `HttpContext`.
- `AddInteractiveServerRenderMode(o => o.ContentSecurityFrameAncestorsPolicy = "'none'")`.
- `POST /_csp/report` endpoint (unauthenticated, body-capped, `204`, structured logging).
- Report-only in `Development`; enforced elsewhere.
- Policy unit tests; header/nonce integration tests.

## 6. Out of scope
Trusted Types (`require-trusted-types-for`/`trusted-types`) — not part of CSP L3 and breaks Blazor's DOM sinks;
`sandbox`; CI wiring.

## 7. Domain concepts
None.

## 8. Layer touchpoints
| Assembly | Change |
|----------|--------|
| Weather.WebUI | `Security/CspMiddleware.cs`, `Security/CspPolicy.cs`, `Security/CspOptions.cs`, `Components/App.razor`, `Program.cs` |
| Weather.WebUI.UnitTests | `Security/CspPolicyTests.cs` |
| Weather.IntegrationTests | `Http/CspHeaderTests.cs` |

## 9. API contract (consumed)
`POST /_csp/report` (own endpoint).

## 10. Dependencies
e06s01 (App.razor/head), e10s01.

## 11. Risks
| Risk | Mitigation |
|------|-----------|
| `'strict-dynamic'` blocks a framework script | Report-Only first; capture at `/_csp/report`; fallback policy without `'strict-dynamic'` |
| Google Fonts origins blocked | Explicit `style-src`/`font-src` origins (D2/Q9) |
| Inline style attributes | `.icon-fill` class; `style-src-attr 'none'` |
| Nonce replay from cache | `Cache-Control: no-store` on nonce-bearing responses |
| Dev hot reload broken | Report-only in `Development` (Q11) |

## 12. Implementation steps
1. Add `CspOptions` + `CspPolicy` and its unit tests → verify: `dotnet test src/Tests/Weather.WebUI.UnitTests --filter Category=CspPolicy`
2. Add `CspMiddleware` (nonce, header, `Reporting-Endpoints`, `Cache-Control`) and register it early → verify: `dotnet test src/Tests/Weather.IntegrationTests --filter Category=CspHeaders`
3. Apply the nonce to `ImportMap` and `blazor.web.js` in `App.razor` → verify: `dotnet test src/Tests/Weather.IntegrationTests --filter Category=CspNonce`
4. Set `ContentSecurityFrameAncestorsPolicy = "'none'"` → verify: `dotnet test src/Tests/Weather.IntegrationTests --filter Category=CspHeaders`
5. Add the `/_csp/report` endpoint (body-capped, `204`, structured logging) → verify: `dotnet test src/Tests/Weather.IntegrationTests --filter Category=CspReport`
6. Add report-only-in-Development behaviour → verify: `dotnet test src/Tests/Weather.IntegrationTests --filter Category=CspEnvironment`
7. Confirm interactivity under enforcement → verify: `npm --prefix src/Weather.WebUI run parity:styles`

## 13. Test plan
- Unit: the policy emits all directives; nonces are unique per request.
- Integration: header present on `/`, `/favorites`, `/not-found`; header nonce equals the rendered script nonce;
  `Reporting-Endpoints` present; report endpoint returns `204`.
- Manual/Playwright: zero CSP console violations; the circuit connects.

## 14. Acceptance criteria (Gherkin)
```gherkin
Scenario: Policy on every page
  Given the app is running outside Development
  When I request "/", "/favorites" and "/not-found"
  Then each response has a Content-Security-Policy header with frame-ancestors 'none'

Scenario: Nonce authorises Blazor
  Given the CSP header contains a nonce
  Then the ImportMap and blazor.web.js script tags carry the same nonce

Scenario: Violations are reportable
  Given a browser posts a CSP report to "/_csp/report"
  Then the endpoint returns 204 and logs the violation
```

## 15. Definition of done
Header on all responses; nonce matches; no CSP violations on both pages; Preflight green.

## 16. Verification script
1. Run the app; open DevTools console on `/` and `/favorites`; confirm zero CSP errors.
2. `curl -sI http://localhost:PORT/ | grep -i "content-security-policy"`.
3. Confirm the report endpoint returns `204`.

## 17. Out of scope
Trusted Types; a monitoring backend; per-page policies.

## 18. Risks / notes
Start in `Content-Security-Policy-Report-Only` (staging) until `/_csp/report` is quiet, then enforce.

## 19. Sizing / BCP rationale
5 BCPs — middleware + policy + nonce wiring + endpoint + tests.

## 20. References
- `specs/tech-architecture/UI_DESIGN_PLAN_LATEST.md` §7b, §9a, §12 (Q10/Q11)
- Microsoft Learn: *Enforce a Content Security Policy for ASP.NET Core Blazor*
