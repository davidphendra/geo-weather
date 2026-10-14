# UI Design Plan — Telemetry Slate for Weather.WebUI

> Status: **PLAN LOCKED** — decisions D1–D6 confirmed. Implementation not started.
> Scope owner: `Weather.WebUI` (host: Blazor UI + REST API). No module changes.
> Change control: this revision records decisions only. No application code written, no commit made.

## 1. Source artifacts

| Artifact | Role |
|----------|------|
| `specs/designs/telemetry_slate/DESIGN.md` | Design-system prose (rationale and component guidance) |
| `specs/designs/weather_explorer_asp.net_core_client/code.html` | Prototype page 1 — **Weather Explorer** |
| `specs/designs/favorites_saved_notes_ef_core_state/code.html` | Prototype page 2 — **Favorites & Saved Notes** |

Both prototypes are static **Tailwind (CDN) + Google Fonts (Geist, JetBrains Mono) + Material Symbols Outlined**, with a JS simulation standing in for the API.

**Acceptance threshold (from the request):** the shipped UI must render with the **same CSS styling** as the prototypes.

## 2. Locked decisions

| # | Decision | Choice |
|---|----------|--------|
| D1 | CSS strategy | **A — Real Tailwind build.** Tailwind **v3.4.x**, `tailwind.config.js` extracted verbatim from `code.html`, source `Styles/tailwind.css`, compiled to `wwwroot/app.css`. npm-based build step (`package.json` + MSBuild target); **compiled output committed** so build/test stay offline. |
| D2 | Fonts & icons | **B — Google Fonts CDN.** Geist, JetBrains Mono, Material Symbols Outlined loaded from `fonts.googleapis.com` / `gstatic.com` via `<link>` in `App.razor`, with local fallback stacks (`system-ui`, `ui-monospace`) so offline rendering degrades gracefully. |
| D3 | Unbacked data | **A — Substitute nearest real metric + explicit "unavailable" states; no fake data.** Backend untouched; forecast stays out of scope. |
| D4 | Token source of truth | **A — `code.html`'s Tailwind config is authoritative.** `DESIGN.md` remains prose/component rationale and is **not modified**; where they conflict, the HTML config wins. |
| D5 | Scope placement | **A — Reshape epic e06** into the Telemetry Slate WebUI epic (5 stories). |
| D6 | Render mode | **A — `@rendermode InteractiveServer`** on `/` and `/favorites`. |
| D7 | Same-origin API addressing | **A — derive the origin from `NavigationManager.BaseUri` at runtime; no `Api:BaseAddress` configuration key.** |
| D8 | Content Security Policy | **A — CSP Level 3 via a global response header, per-request nonce + `'strict-dynamic'`, enforced on every page; report-only in `Development` unless overridden.** |

## 3. Design tokens (binding — from `code.html`)

- Colors (Material-3 blue set): `surface #f8f9ff`, `surface-container-lowest #ffffff`, `surface-container-low #eff4ff`, `surface-container #e5eeff`, `surface-container-high #dce9ff`, `surface-container-highest #d3e4fe`, `primary #000000`, `primary-container #131b2e`, `on-primary #ffffff`, `on-primary-container #7c839b`, `secondary #006398`, `secondary-fixed #cce5ff`, `secondary-container #5bb8fe`, `on-secondary-fixed-variant #004b73`, `tertiary-fixed #c4e7ff`, `tertiary-fixed-dim #7bd0ff`, `on-surface #0b1c30`, `on-surface-variant #45464d`, `outline #76777d`, `outline-variant #c6c6cd`, `error #ba1a1a`, `error-container #ffdad6`.
- Radii: `DEFAULT .125rem`, `lg .25rem`, `xl .5rem`, `full .75rem`.
- Spacing: `space-xs .25rem`, `space-sm .5rem`, `space-md .75rem`, `space-lg 1rem`, `space-xl 1.5rem`, `gutter 1rem`, `gutter-desktop 1.5rem`, `margin 1rem`, `margin-desktop 2rem`.
- Type scale (family / size / line-height / tracking / weight): `headline-xl|lg|md|sm`, `body-lg|md|sm`, `code-lg|md|sm`, `label-md|sm` (exact values per `code.html`).
- Fonts: **Geist** (editorial/UI), **JetBrains Mono** (all numerics, code, chips).
- Elevation: border-driven (`1px solid`, surface tiers) + token shadows `shadow-sm`, `shadow-[0_1px_8px_rgba(0,0,0,0.04)]`, hover/overlay depths.

## 4. Information architecture

### Header (shared, fixed)
`bg-primary-container`, 64px, logo → `GeoWeather` + framework badge + version; nav links *Weather Explorer* (`/`), *Favorites & Saved Notes* (`/favorites`). Content correction: badge shows **.NET 10** (project is `net10.0`), version from the assembly.

### Footer (shared)
`bg-surface-container-lowest`, `font-code-sm`: host/runtime/GC/latency left; ASP.NET Core / OpenAPI / © right.

### Pages
1. **`/` Weather Explorer** — query controller panel + MediatR bar; hero station card + diagnostic grid; forecast strip; command dispatch form + response well; architecture drawer.
2. **`/favorites` Favorites & Saved Notes** — metrics strip; control bar; entity table; quick-add mutation form; CQRS feed; pipeline topology; JSON inspector modal.

### Removals
Delete template artifacts: `Counter.razor`, `Weather.razor` (superseded), sidebar/`NavMenu` styling, bootstrap CSS/JS if unused, template `MainLayout`/`app.css`.

## 5. Component inventory (Razor)

| Group | Components |
|-------|-----------|
| Shell | `AppHeader`, `AppFooter`, `AppNav`, `AppShell` (layout) |
| Design system | `StatChip`, `StatusPill`, `MethodBadge`, `SectionCard`, `CodeWell`, `TabSwitcher`, `TextField`, `SelectField`, `TextAreaField`, `Button`, `TagChip`, `TerminalLogWell` |
| Weather Explorer | `QueryControllerPanel`, `MediatRExpressionBar`, `StationHeroCard`, `DiagnosticMetricGrid`, `Sparkline`, `ForecastStrip`, `ForecastDayCard`, `CommandDispatchForm`, `ChangeTrackerResponseWell`, `ArchitectureDeepDive` |
| Favorites | `MetricsStrip`, `EntityControlBar`, `EntityTable`, `EntityRow`, `TypeBadge`, `SnapshotCell`, `QuickAddCommandForm`, `CqrsMutationFeed`, `PipelineTopologyCard`, `RawJsonInspectorModal` |
| Services | `WeatherApiClient`, client DTO mirrors, page view-models (formatting: °C/°F, cardinal, hPa, visibility km, timestamps) |

## 6. Data mapping (real API only, no fake data)

| Prototype element | Source | Policy |
|---|---|---|
| Country / city selects | `GET /api/countries`, `GET /api/countries/{code}/cities` | Wire |
| Station name / coords | `WeatherReportDto.Location` + `CityDto` lat/long | Wire |
| Temp C/F, condition, humidity, dew point, wind, visibility, pressure | `WeatherReportDto` | Wire |
| **UV index / solar** cell | none | Substitute → **Visibility** (`VisibilityMeters` → km/mi), same grid slot |
| Wind "Gusts" line | none (speed + cardinal only) | Substitute → real **direction/cardinal** |
| Pressure "Steady +0.2" | none | Substitute → **observed-at UTC** timestamp |
| **24h trend / sparkline + "Trend: +1.2°"** | none (current only) | Drop the fabricated series; keep a header noting **current observation only** |
| **5-day forecast strip** | none (out of scope) | Keep the strip shell; render one explicit **"Forecast not available — current conditions only"** state |
| Command dispatch | `POST /api/weather/notes`, `POST /api/cities/favorites` | Wire |
| ChangeTracker / response well | actual HTTP status + response JSON | Render real response |
| Architecture tabs (cURL/C#) | static documentation | Static content, real route paths |
| Favorites list | `GET /api/cities/favorites` | Wire |
| **Notes list / notes count / tags** | none (POST only) | Render **Not available**; notes count shows `—` |
| "Tracked Entities" / "Last Mutation" stat cards | favorites + last client request | Derive from loaded data |
| CQRS feed | client request results | Log real calls |

Endpoint labels in the UI show the **real** routes (`/api/countries`, `/api/weather/{cityName}`, `/api/weather/notes`, `/api/cities/favorites`), not the prototype's `/api/v1/...`.

## 7. Implementation approach (locked)

- `src/Weather.WebUI/tailwind.config.js` — token block copied verbatim from `code.html`.
- `src/Weather.WebUI/Styles/tailwind.css` — `@tailwind base/components/utilities` plus any minimal base layer from the prototype (`html,body{margin:0}`, scrollbar rule).
- Tailwind **v3.4.x** via npm; `package.json` + an MSBuild target compile to `wwwroot/app.css`; the compiled CSS is committed so `dotnet build`/`test` require no network.
- Razor markup uses the same utility classes as the prototypes (`p-space-lg`, `font-code-sm`, `bg-surface-container-lowest`, `rounded-xl`, `shadow-[…]`, `max-w-[1600px]`, …).
- Fonts/icons via `App.razor` `<link>` tags (D2) with local fallback stacks.
- `MainLayout` = header + `<main>` + footer (no sidebar); `<html>`/`<body>` carry `bg-surface font-body-md text-body-md text-on-surface antialiased min-h-screen flex flex-col`.
- Feature pages use `@rendermode InteractiveServer` (D6).
- No CSS isolation (`*.razor.css`) for design-system styles.

## 7a. Same-origin HTTP (D7 rationale)

`InteractiveServer` means component code runs **server-side**, so the browser's relative-URI resolution does not apply.
Microsoft Learn, *Call a web API from ASP.NET Core Blazor*:

- Client-side (WASM/`Auto`): the `HttpClient` is preconfigured — *"The client's base address is set to the originating server's address"* — so relative paths such as `"api/countries"` resolve automatically.
- Server-side: *"A server-side app doesn't include an `HttpClient` service. Provide an `HttpClient` to the app using the `HttpClient` factory infrastructure"* (`AddHttpClient()`), and *"On the server, `builder.HostEnvironment.BaseAddress` is replaced by the web API's base address."* A server-side `HttpClient` with no `BaseAddress` and a relative URI throws `InvalidOperationException`.

Therefore:

- **No `Api:BaseAddress` configuration key.** The UI and the API share one origin (ADR-0012), so there is nothing to configure.
- Register the HTTP client with `IHttpClientFactory` (`builder.Services.AddHttpClient<WeatherApiClient>()`) per the server-side guidance.
- Resolve the origin at runtime from `NavigationManager.BaseUri` and build absolute request URIs against it. This respects `<base href="/">` and sub-path/reverse-proxy hosting.
- Tests feed `WebApplicationFactory.CreateClient()` (the in-memory test origin), so no configuration is needed there either.

**Noted alternative (not chosen):** because the API is in-process, the interactive pages could inject the module contracts (`I<Module>Service`) directly and avoid the HTTP loop entirely (Microsoft: interactive SSR needs no API endpoints to access server resources). ADR-0012 explicitly rejected in-process calls in favour of "consume the API contract"; this plan keeps HTTP and only corrects how the origin is obtained.

## 7b. Content Security Policy Level 3 (D8)

Applied **centrally to every response** with a `Content-Security-Policy` **response header** (not a `<meta>` tag), because a header is required for `frame-ancestors`, `report-to`, and `sandbox` (Microsoft: *a `<meta>` tag policy doesn't support `frame-ancestors`, `report-to`, `report-uri`, `sandbox`*). This also satisfies “all pages” in one place rather than per-layout meta tags.

### Delivery

- New `Weather.WebUI.Security/CspMiddleware` (or `app.Use(...)`) generates a cryptographically random **per-request nonce** (`RandomNumberGenerator`, 32 bytes, Base64), stores it in `HttpContext.Items`, and emits:
  - `Content-Security-Policy: <policy>` (or `Content-Security-Policy-Report-Only` in `Development` unless `Csp:EnforceInDevelopment=true`);
  - `Reporting-Endpoints: csp-endpoint="/_csp/report"` (CSP Level 3 `report-to` group).
- A minimal `POST /_csp/report` endpoint logs violation reports with `ILogger<CspReportEndpoint>` (structured, no PII), caps the body size, and returns `204`.
- `App.razor` reads the nonce from the cascading `HttpContext` and applies it to the two inline/`self` scripts that must run: `<ImportMap nonce="@Nonce" />` and `<script src="@Assets["_framework/blazor.web.js"]" nonce="@Nonce"></script>`. (Microsoft documents exactly this nonce route for the `ImportMap` component.)
- `frame-ancestors 'none'` is also set through the framework: `.AddInteractiveServerRenderMode(o => o.ContentSecurityFrameAncestorsPolicy = "'none'")` (Blazor Web Apps default to `'self'`; this tightens it).

### Policy (target)

```
base-uri 'self';
default-src 'self';
object-src 'none';
script-src 'self' 'nonce-{N}' 'strict-dynamic';
script-src-elem 'self' 'nonce-{N}' 'strict-dynamic';
script-src-attr 'none';
style-src 'self' https://fonts.googleapis.com;
style-src-elem 'self' https://fonts.googleapis.com;
style-src-attr 'none';
font-src https://fonts.gstatic.com;
img-src 'self' data:;
connect-src 'self' ws: wss:;
form-action 'self';
frame-ancestors 'none';
manifest-src 'self';
upgrade-insecure-requests;
report-to csp-endpoint;
report-uri /_csp/report;
```

### CSP Level 3 features used

| Feature | Why |
|---|---|
| Per-request **nonce** | Authorises the Blazor bootstrap script + `ImportMap` without `'unsafe-inline'` |
| **`'strict-dynamic'`** | Trust propagates only from nonced scripts; host/scheme allowlists for `script-src` are ignored |
| `script-src-elem` / `script-src-attr` / `style-src-elem` / `style-src-attr` | Level 3 granularity; `*-attr 'none'` forbids inline event handlers and `style=` attributes |
| `frame-ancestors 'none'` | Clickjacking defence (header-only) |
| `form-action 'self'`, `base-uri 'self'`, `object-src 'none'` | Level 3 hardening |
| `report-to` + `Reporting-Endpoints` (with `report-uri` fallback) | Level 3 violation reporting |

### Deliberately excluded

- **`require-trusted-types-for 'script'` / `trusted-types` (Trusted Types).** A separate specification, not part of CSP Level 3, and **not** in Microsoft's Blazor CSP guidance. Blazor's client framework uses DOM sink APIs, so enforcing it will break the circuit. If wanted later, evaluate under `Content-Security-Policy-Report-Only` first.
- **`sandbox`.** Would break Blazor's interactivity.

### Interactions with earlier decisions

- **D2 (Google Fonts CDN)** forces `style-src https://fonts.googleapis.com` and `font-src https://fonts.gstatic.com`. Self-hosting the fonts would remove both origins and tighten the policy.
- The prototypes use inline `style="font-variation-settings: 'FILL' 1;"` on Material Symbols. **Convert these to a `style-src-attr 'none'`-safe class** (e.g. `.icon-fill`) so no inline `style` attribute is needed.
- Prototype inline JS event handlers are not ported (Blazor `@onclick` only); the template `NavMenu` inline `onclick` is deleted, so no `'unsafe-hashes'` script hash is needed.
- `Virtualize` is not used, so the inline-spacer-style caveat in the Blazor CSP article does not apply.

### Environment behaviour

- `Production` / `Staging`: **enforced**.
- `Development`: `Content-Security-Policy-Report-Only` by default so Browser Link / hot reload keep working; set `Csp:EnforceInDevelopment=true` to enforce locally.

## 8. Epic / story plan (locked — reshape e06)

Reshape the existing **e06 WebUI Front-End** epic; its current stories are `todo` and unimplemented, so nothing is lost.

| Story | Deliverable |
|-------|-------------|
| **e06s01** Design system foundation + app shell | Tailwind v3.4 config + committed compiled CSS + Google-Fonts links; `AppHeader`/`AppNav`/`AppFooter`/`MainLayout`/`App.razor` shell; token contract test; remove template UI |
| **e06s02** `WeatherApiClient` + country→city cascade | Existing story intent preserved |
| **e06s03** Weather Explorer page | Query controller, hero + diagnostics, forecast shell, command form + response well, architecture drawer |
| **e06s04** Favorites & Saved Notes page | Metrics strip, entity table, quick-add form, CQRS feed, topology, JSON modal |
| **e06s05** Visual-parity verification | Token assertions + screenshot diff vs prototypes at 3 viewports |
| **e06s06** CSP Level 3 | Nonce middleware + header on all responses, `ImportMap`/Blazor-script nonces, `frame-ancestors 'none'`, report endpoint, policy + header + violation tests |

Bookkeeping to update when stories are written: `specs/epics/e06-webui/epic.yaml` (`total_bcps`, note, story list), `specs/execution-status.yaml`, `specs/release-plan.yaml`.

## 9. Verification of the "same CSS" threshold

1. **Token contract tests** — assert the compiled CSS declares all tokens and the component utility classes used by the prototypes.
2. **Screenshot parity harness** (script, not `dotnet test`) — render `specs/designs/*/code.html` and the running app at 390/768/1600 px, diff, thresholded.
3. **Manual visual review** — side-by-side at the three viewports, recorded in `specs/verifications/`.

## 9a. Verification of the CSP (D8)

1. **Policy unit tests** — the policy builder emits every directive, the nonce is unique per request, and `frame-ancestors`/`report-to` are present.
2. **Integration tests** (`WebApplicationFactory<Program>`) — assert the `Content-Security-Policy` header is present on `/`, `/favorites`, and `/not-found`; assert the nonce in the header **matches** the nonce on the rendered `blazor.web.js` script and `ImportMap`; assert `Reporting-Endpoints` is present.
3. **Playwright** — load `/` and `/favorites`, assert **zero CSP console violations** and that the Blazor circuit connects (interactivity works).
4. **Rollout** — run `Content-Security-Policy-Report-Only` in staging until the report endpoint is quiet, then enforce.

## 10. Risks

- Tailwind build adds a Node/npm toolchain to a .NET solution (mitigate: compiled CSS committed; build step additive).
- D2 keeps a runtime font CDN dependency — offline runs use fallback fonts (accepted, documented).
- Prototype content (Telemetry/CQRS/EF internals) is illustrative; UI text must not claim behaviour the app does not have.
- Scope of e06 grows from 2 to 6 stories (raise `e06.total_bcps` and WSJF).
- **CSP `'strict-dynamic'` + Blazor**: the policy may need tuning if the framework injects scripts not covered by the nonce. Mitigation: start `Report-Only`, capture violations at `/_csp/report`, adjust; fallback policy drops `'strict-dynamic'` and uses `script-src 'self' 'nonce-…'` plus hashes.
- **D2 (font CDN) widens the CSP**: `https://fonts.googleapis.com` (style) and `https://fonts.gstatic.com` (font) must be allowed. Self-hosting would remove this and is the tighter option.
- **Nonce vs. caching**: any response carrying a per-request nonce must not be cached and replayed with a mismatched header. Mitigation: CSP middleware marks HTML responses `Cache-Control: no-store` (or the nonce is embedded consistently per response).

## 12. Session decision log (Q1–Q12)

| # | Question | Decision |
|---|----------|----------|
| Q1 | Write the e06 capsule now? | **A — yes, via `plan-work`** (planning only; no code) |
| Q2 | Branch | **A — new `feature/e06-webui` off `feature/e10-single-host` via `kickoff-branch`** |
| Q3 | Branding | **A — keep “GeoWeather”**; only `.NET 8`→`.NET 10` and the year change |
| Q4 | Command form intent | **A — single form with Favorite ⇄ Note toggle (default Favorite)** |
| Q5 | Logo | **A — local placeholder SVG** `wwwroot/img/geoweather-mark.svg` |
| Q6 | Favorites type filter | **A — `All | Favorites`** (matches export) |
| Q7 | Tailwind toolchain | **A — `src/Weather.WebUI/package.json`, npm build, committed `app.css`; include the optional CI drift guard** |
| Q8 | Visual-parity gate | **B — Playwright computed-style + geometry assertions; screenshots as artifacts; token tests in `dotnet test`** |
| Q9 | Fonts for the harness | **A — harness may fetch the Google fonts; `dotnet test` stays offline** |
| Q10 | CSP report route | **A — `/_csp/report`** (non-API path) |
| Q11 | CSP in Development | **A — report-only in `Development`; enforced elsewhere; `Csp:EnforceInDevelopment` override** |
| Q12 | Accessibility | **A — pragmatic semantics/keyboard support, no formal WCAG audit, no token changes; report contrast findings** |
