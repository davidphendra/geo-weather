# ADR-0020 — Fluent UI Blazor for the searchable country/city pickers

**Status:** Accepted
**Date:** 2026-10-15

## Context
The Weather Explorer (`/`) selects a country and then a city. Both were native `<select>` elements
(`Components/Explorer/QueryControllerPanel.razor`). The Geography catalog holds **249 countries**
ordered by name, so the country popup is long and cannot be filtered by typing. The request:
about 10 rows visible with scroll for the rest, an in-place text filter, and a configurable default
country/city (Indonesia/Jakarta).

The host enforces a strict CSP Level 3 header (`Weather.WebUI/Security/CspPolicy.cs`):
`script-src 'self' 'nonce-…'`, `style-src-elem 'self' …`, and **`style-src-attr 'none'`**. The
Tailwind "Telemetry Slate" design system and its Playwright parity gate
(`tests/visual/parity.spec.ts`) probe the native `<select>` and assert **zero CSP violations**.

## Provider / library facts (verified against the package + repo docs, 2026-10-15)
- `Microsoft.FluentUI.AspNetCore.Components` (MIT, Microsoft-published) 5.0.1 targets `net10.0`.
- Registration: `builder.Services.AddFluentUIComponents()`. The web-component script is auto-included
  as a **Blazor JS initializer** served from the app origin — no CDN, and it satisfies `script-src 'self'`.
- `FluentCombobox<TOption,TValue>` filters its `Items` as the user types (`Immediate` /
  `ImmediateText`), supports `Multiple="false"`, and is documented for very long lists. Its `Height`
  parameter caps the popup.
- `FluentAutocomplete<TOption,TValue>` is the alternative for async/server search, but its docs state
  "Accessibility requirements are not yet implemented", so it was not chosen.
- **Spike finding:** Fluent's `Height` parameter renders `style="max-height: 16rem;"` on the inner
  `<fluent-listbox>`. With `style-src-attr 'none'` this produced three Playwright CSP violations
  ("Refused to apply inline style"). Moving the cap to a stylesheet rule removed the inline style and
  the harness passed 7/7 with zero violations. Fluent emitted **no other** inline styles.

## Decision
1. **D1 — Adopt Fluent UI Blazor for the two location pickers only.** Pin
   `Microsoft.FluentUI.AspNetCore.Components` 5.0.1 in `Directory.Packages.props`; keep the rest of
   the Telemetry Slate UI and its Tailwind design system unchanged. Do **not** add
   `FluentDesignSystemProvider` or `reboot.css` (no global restyle).
2. **D2 — Country picker:** `FluentCombobox` single-select with `Immediate` client-side filtering
   over the already-loaded `CountryModel` list. No new API call; `GET /api/countries?search=` stays
   available for a later server-side switch.
3. **D3 — City picker:** the same `FluentCombobox` pattern over `Cities` (≤ 5 rows, so the cap never
   engages). Both pickers stay visually consistent.
4. **D4 — Popup height:** cap via the stylesheet rule
   `.location-combobox fluent-listbox { max-height: 16rem; }` in `Styles/tailwind.css`. Do **not** use
   Fluent's `Height` parameter — it is CSP-unsafe under `style-src-attr 'none'`.
5. **D5 — Defaults:** `LocationOptions` (`Location:DefaultCountryCode` / `DefaultCityName`);
   shipped as `ID` / `Jakarta`, overridable per environment. Blank/unknown values fall back to the
   first country/city.
6. **D6 — CSP unchanged:** no new origins; the Fluent script is same-origin. The inline-style hazard
   is avoided by D4.
7. **D7 — Parity gate:** replace the native-`select` computed-style probe (which no longer exists)
   with a probe on the query card; keep the zero-CSP assertion; add a Playwright spec that exercises
   the filter and the ~10-row cap.
8. **D8 — Component choice:** `FluentCombobox`, not `FluentAutocomplete`, to avoid the documented
   accessibility gap.
9. **D9 — Global reset override:** Fluent's client library adopts a global stylesheet on
   `document.adoptedStyleSheets` (independent of `<FluentDesignSystemProvider>`) that sets
   `body { height: 100dvh; overflow: hidden }` and `a { text-decoration: underline }`. The body rule
   propagates to the viewport and blocks user scrolling (BUG-002). Override both in the Tailwind base
   layer with higher-specificity selectors rather than `!important`:
   `html body { height: auto; min-height: 100dvh; overflow: visible }` and
   `html a { text-decoration: inherit }`. Regression test: `tests/visual/scroll.spec.ts`.

## Rationale
- The requirement is a filterable, height-capped picker; `FluentCombobox` provides both and is
  explicitly recommended for long option lists.
- Scoping Fluent to the pickers avoids re-skinning the whole app and keeps the parity gate meaningful
  everywhere else.
- Keeping filtering client-side is the minimum code: the 249-item list is already loaded, so no
  debounce/cancellation plumbing and no extra requests.
- The stylesheet cap reuses the exact mechanism Fluent intended (`max-height` on `fluent-listbox`)
  without the inline style.

## Consequences
- Positive: the country popup is filtered and capped; defaults are configurable; the strict CSP is
  preserved; no new CSP origins.
- Negative: the two pickers intentionally diverge from the prototype's native-select styling, so the
  parity gate no longer asserts their computed style. The pickers' browser behaviour (filter, scroll,
  selection) is not covered by offline dotnet tests — it is covered by the Playwright spec.
- Risk: a Fluent UI major upgrade could reintroduce inline styles or change the internal
  `fluent-listbox` element; the Playwright CSP assertion guards this. A Fluent upgrade could also change
  the adopted global reset (D9); `tests/visual/scroll.spec.ts` guards the body/link overrides.

## Alternatives rejected
- **Fluent's `Height` parameter:** CSP violation under `style-src-attr 'none'` (spike evidence).
- **Relax CSP to `style-src-attr 'unsafe-inline'`:** weakens the locked D8 policy for a cosmetic cap.
- **`FluentAutocomplete`:** documented accessibility gap.
- **Native `<input list>` + `<datalist>`:** no dependency and no design divergence, but the popup is
  browser-controlled and cannot be Fluent-shaped; rejected in favour of the requested library.
- **Hand-rolled Tailwind combobox:** the user asked not to build one from scratch.

## References
- `specs/tech-architecture/SEARCHABLE_LOCATION_SELECT_PLAN_LATEST.md`
- `specs/bugs/BUG-002-unscrollable-page-fluent-body-reset.md`
- `specs/tech-architecture/UI_DESIGN_PLAN_LATEST.md` (D1, D4, D8)
- `specs/adr/0011-rest-api-response-contract.md`
- Fluent UI Blazor: `https://www.fluentui-blazor.net/Combobox`
