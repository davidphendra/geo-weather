# BUG-002: Pages cannot be scrolled with the wheel/trackpad after adopting Fluent UI

## Problem

- **Actual:** Since Fluent UI Blazor was adopted for the country/city pickers, the page cannot be
  scrolled by user input (mouse wheel, trackpad, touch, or scrollbar). On the Weather Explorer with a
  full report, the footer — which shows the app version — is unreachable at the bottom.
- **Expected:** the page scrolls normally and the footer is reachable.
- **Reproduction:** run the host with the CSP enforced, open `/` (also reproduces on `/favorites`),
  and use the wheel. The viewport does not move even though
  `documentElement.scrollHeight - innerHeight > 0`.
- **Detection gap:** programmatic `window.scrollTo(...)` still moves the viewport, so the visual-parity
  harness and earlier checks appeared to pass. The symptom only shows with a real wheel/touch event.

Security impact: **NONE** — a CSS side effect, no exploit path.

## Root Cause Analysis

The Fluent UI Blazor client library adopts a global stylesheet onto `document.adoptedStyleSheets`
during initialisation, independent of whether `<FluentDesignSystemProvider>` is rendered. One of its
rules is a Fluent 2 base reset:

```
body { height: 100dvh; overflow: hidden; font-family: var(--fontFamilyBase); ... }
```

Because the root (`html`) element is `overflow: visible`, CSS overflow propagation promotes the body's
`overflow: hidden` to the **viewport**. A viewport with `overflow: hidden` still supports programmatic
scrolling but rejects user scroll input, which is exactly the observed behaviour.

The same global reset also force-underlines every link
(`a { color: var(--colorBrandForegroundLink); text-decoration: underline; }`), diverging from the
Telemetry Slate design, which relies on Tailwind Preflight (`a { text-decoration: inherit }`).
Table-cell padding is **not** a regression — the design sets it via Tailwind utility classes, which
outrank Fluent's type selector.

Contributing factor: the reset is injected by the library with no `LibraryConfiguration` switch to
disable it (checked `AddFluentUIComponents` and `LibraryConfiguration`). Overriding in the app's own
stylesheet is therefore the supported-feeling remedy.

Risk level: **Medium** (site-wide UX regression, no data loss, no security impact).

Regression origin: ADR-0020 (Fluent UI pickers). Before that change the body had no `height`/`overflow`
constraint.

## TDD Fix Plan

1. **RED**: Add a Playwright regression test: on a tall page, a real `page.mouse.wheel(...)` advances
   `window.scrollY` and the `footer` becomes visible; also assert `getComputedStyle(document.body).overflowY`
   is not `hidden` (the current bug leaves `scrollY` at 0).
   **GREEN**: In the Tailwind base layer add
   `html body { height: auto; min-height: 100dvh; overflow: visible; }`.
   Specificity (`html body` = two type selectors) beats the adopted `body` rule regardless of adopted
   style-sheet ordering, so no `!important` is needed.
   **verify**: `npm --prefix src/Weather.WebUI run parity:styles`
   (wheel fails before the change, passes after; validated in-browser).

2. **RED**: Extend the test (or add a focused check) that links keep the design's decoration —
   `getComputedStyle(a).textDecorationLine` is `none` for the nav links.
   **GREEN**: In the base layer add `html a { text-decoration: inherit; }` so Fluent's forced underline
   does not apply (Tailwind's explicit `hover:underline` still wins on hover).
   **verify**: `npm --prefix src/Weather.WebUI run parity:styles`

3. **REFACTOR**: Note the global-reset constraint and both overrides in ADR-0020; rebuild and commit
   `wwwroot/app.css` (`npm --prefix src/Weather.WebUI run build:css`, guarded by
   `scripts/check-tailwind-css.sh`).

## Acceptance Criteria

- [ ] A real wheel/trackpad/touch scroll moves the Weather Explorer page and the footer (with the
      version) is reachable.
- [ ] `document.body` is not `overflow: hidden`; `scrollHeight > innerHeight` pages are user-scrollable.
- [ ] Navigation links are not force-underlined.
- [ ] All new tests pass; existing dotnet and Playwright suites stay green; the CSP gate stays green.

## Task list

Every task ships a runnable `verify:` and a risk (`P0`–`P3`). F1 and F3 are intentionally RED before
their GREEN counterpart lands; the branch is only green at F5.

| ID | Task | verify | risk |
|----|------|--------|------|
| F0 | Create the fix branch from an up-to-date, clean `main`. | `git branch --show-current` → `fix/scroll-fluent-body-reset` | P1 |
| F1 | **RED** — Add `tests/visual/scroll.spec.ts`: on a tall page (`/favorites`), a real `page.mouse.wheel(0, 600)` advances `window.scrollY`, the `footer` becomes visible, and `getComputedStyle(document.body).overflowY !== 'hidden'`. | `npm --prefix src/Weather.WebUI run parity:styles` → new spec fails on the current tree | P0 |
| F2 | **GREEN** — In the Tailwind base layer add `html body { height: auto; min-height: 100dvh; overflow: visible; }`; rebuild `wwwroot/app.css`. | `npm --prefix src/Weather.WebUI run parity:styles` → spec passes | P0 |
| F3 | **RED→GREEN** — Extend the spec to assert the nav links are not force-underlined (`text-decoration-line: none`), then add `html a { text-decoration: inherit; }`; rebuild CSS. | `npm --prefix src/Weather.WebUI run parity:styles` | P2 |
| F4 | Record the Fluent global-reset constraint and both overrides in ADR-0020. | `grep -q adoptedStyleSheets specs/adr/0020-*.md` | P2 |
| F5 | Full gate: dotnet preflight, CSS drift guard, Playwright (incl. the zero-CSP assertion), layout guard. | `dotnet build Weather.sln --warnaserror && dotnet format Weather.sln --verify-no-changes && dotnet test Weather.sln` then `bash scripts/check-tailwind-css.sh && npm --prefix src/Weather.WebUI run parity:styles && bash scripts/check-layout.sh` | P0 |
| F6 | Close the record: fill BUG-002 §Resolution; set `registry.yaml` `status: fixed` with evidence. | `grep -q 'id: BUG-002' specs/bugs/registry.yaml && grep -q 'status: fixed' specs/bugs/registry.yaml` | P2 |
| F7 | Ship: merge the fix branch to `main` (`--no-ff`). | `git log --oneline -1 main` shows the merge commit | P1 |

Out of scope: reverting Fluent UI (the pickers work); the body/link overrides are the minimal,
reversible remedy. A real-browser wheel/trackpad pass on `/` with a full report is covered by F1's
real-event assertion.

## Resolution

<!-- filled in by validate-fix -->
