# Story e06s05 — Visual-parity verification

| Field | Value |
|-------|-------|
| ID | e06s05 |
| Epic | e06 WebUI Front-End |
| Status | todo |
| BCPs | 5 |
| Risk | P1 |
| Type | test |

## 1. Summary
Prove the shipped pages render with the **same CSS** as the prototypes, using a Playwright harness that asserts
computed styles and layout geometry against `specs/designs/*/code.html`, plus offline token-contract tests.

## 2. Context
The request's threshold is "the result needs to have the same CSS styles as the proposed designs". CSS-text
assertions alone cannot prove resolution; a browser is required. To avoid flakiness, the gate compares
`getComputedStyle` values and bounding boxes rather than raw pixels; screenshots are captured as review artifacts.

## 3. User story
As a reviewer, I want an automated, reproducible check that the app's CSS matches the prototypes.

## 4. Business value
Makes "same CSS" a gate, not an opinion.

## 5. In scope
- Playwright dev-dependency and a harness script (`npm run parity:styles`).
- Harness starts the app, loads `/` and `/favorites`, and compares matched elements (header, containers, cards,
  chips, buttons, table, grid columns) against the prototypes at 390/768/1600 px.
- Assertions: computed style equality on styled properties + geometry tolerance; screenshots saved.
- Token-contract tests remain in `dotnet test` (offline).
- Harness may fetch the Google fonts (Q9) for a fair comparison.

## 6. Out of scope
Pixel-diff as a hard gate; axe/WCAG auditing (Q12); visual testing in CI wiring (see `wire-ci`).

## 7. Domain concepts
None.

## 8. Layer touchpoints
| Assembly | Change |
|----------|--------|
| Weather.WebUI | `package.json` devDependency, `tests/visual/*` harness |
| Weather.WebUI.UnitTests | token-contract test (from e06s01) referenced |

## 9. API contract (consumed)
`GET /`, `GET /favorites` (page HTML).

## 10. Dependencies
e06s01, e06s03, e06s04.

## 11. Risks
| Risk | Mitigation |
|------|-----------|
| Font CDN offline → both sides fall back and still match, but not the design | Harness is allowed network (Q9); artifacts record the font state |
| Flaky geometry on font load | Wait for `document.fonts.ready` before measuring |
| Proof only covers sampled elements | Maintain the element map alongside `.icon-fill`/token list |

## 12. Implementation steps
1. Add `@playwright/test` to `src/Weather.WebUI/package.json` → verify: `npm --prefix src/Weather.WebUI install`
2. Add the harness that boots the app on a fixed port and waits for readiness → verify: `npm --prefix src/Weather.WebUI run parity:styles`
3. Add the prototype-vs-app element map and computed-style assertions → verify: `npm --prefix src/Weather.WebUI run parity:styles`
4. Add geometry assertions (container widths, 7/5 and 8/4 column splits, 64px header) → verify: `npm --prefix src/Weather.WebUI run parity:styles`
5. Capture screenshots at 390/768/1600 px as artifacts → verify: `test -d src/Weather.WebUI/tests/visual/artifacts`
6. Record the result in `specs/verifications/e06s05.md` → verify: `test -f specs/verifications/e06s05.md`

## 13. Test plan
- Computed-style equality for the sampled element set.
- Geometry tolerance for grid splits and the header.
- Token-contract test in `dotnet test` remains green and offline.

## 14. Acceptance criteria (Gherkin)
```gherkin
Scenario: Styles match the prototype
  Given the app and the prototype are rendered at 1600px
  When I compare the sampled elements
  Then their computed styles are equal and geometry is within tolerance

Scenario: Offline gate stays offline
  Given the network is unavailable
  When I run dotnet test
  Then the token-contract tests still pass
```

## 15. Definition of done
Harness green at all three viewports; artifacts attached; `specs/verifications/e06s05.md` written.

## 16. Verification script
1. `npm --prefix src/Weather.WebUI run parity:styles`.
2. Inspect `specs/verifications/e06s05.md` and the screenshots.

## 17. Out of scope
Pixel diffs; cross-browser matrix (chromium only); CI wiring.

## 18. Risks / notes
This harness is not part of `dotnet test`/Preflight; it is a separate verification script (Q7/Q8).

## 19. Sizing / BCP rationale
5 BCPs — harness + element map + assertions + evidence.

## 20. References
- `specs/tech-architecture/UI_DESIGN_PLAN_LATEST.md` §9, §12 (Q8/Q9)
- Playwright computed-style comparison approach
