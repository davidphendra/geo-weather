# Searchable Country / City Select — Plan

Status: **PLAN — decision required** before implementation.
Scope owner: `Weather.WebUI` (host: Blazor UI). Backend already supports the needed filter.

## 1. Problem

On `/` (`WeatherExplorer.razor` → `QueryControllerPanel.razor`) the country and city pickers are native
`<select>` elements. The Geography catalog holds **249 countries**, ordered by name (Afghanistan first),
so the country popup is long and awkward to use, and it cannot be filtered by typing.

Requested behaviour:
1. Show about **10** rows at a time; the rest are reachable by **scroll**.
2. A **text filter** inside the picker.
3. Default selection = **Indonesia** (country) and **Jakarta** (city), **configurable** (not compiled in).
4. Prefer a free/OSS component library (Fluent UI Blazor) over a bespoke control.

## 2. Current facts (verified)

| Fact | Detail |
|------|--------|
| Data volume | 249 countries (`Migrations/Seed/countries.sql`); up to 5 cities per country (`cities.sql`, 977 rows). |
| Indonesia / Jakarta | Present: `('ID','Indonesia')`, Jakarta city (`ID`). |
| API filter | `GET /api/countries?search=` already filters by name (`CountryReadRepository`: `LIKE %term%`, `AsNoTracking`, ordered by name). |
| Service contract | `IGeographyService.GetCountriesAsync(string? search, CancellationToken)`. |
| WebUI client | `WeatherApiClient.GetCountriesAsync(CancellationToken)` does **not** pass `search` yet. |
| Initial selection | `WeatherExplorerModel.InitializeAsync` loads all countries, then picks `Countries[0]` (Afghanistan) and `Cities[0]`. No defaults are configurable today. |
| Config pattern | `CspOptions`/`DatabaseOptions` in `Weather.WebUI/Configuration`, bound in `AddWeatherUi`; `appsettings.json` per environment; `__` env-var overrides. |
| Design system | Locked Tailwind "Telemetry Slate" (Material-3 tokens, Geist/JetBrains Mono). UI parity gate `tests/visual/parity.spec.ts` probes `section:has(select)` and `select` and asserts **zero CSP violations**. |
| CSP | Strict, header-based: `script-src 'self' 'nonce-…'`, `style-src-attr 'none'`, `style-src-elem 'self' …`. Inline `style=""` attributes are blocked. |

## 3. Fluent UI Blazor — what the docs/package say (v5.0.1 / v4.14.5, MIT, `net10.0`)

Package: `Microsoft.FluentUI.AspNetCore.Components`. Registration: `builder.Services.AddFluentUIComponents()`.
The web-component script (`…lib.module.js`) is **auto-included as a Blazor JS initializer** (same origin → allowed by `script-src 'self'`); no CDN, no manual `<script>` required. `FluentDesignSystemProvider` and `reboot.css` are optional.

Relevant components:

| Component | Text filter | Capped scroll | Notes |
|-----------|-------------|---------------|-------|
| `FluentSelect<TOption,TValue>` | No | `Height` (max-height) | Select only. |
| `FluentCombobox<TOption,TValue>` | Yes (`Immediate`/`ImmediateText`, filtered `Items`) | `Height` (inherited from Select) | `Multiple="false"` supported; "good for very long lists". Closest to the current dropdown. |
| `FluentAutocomplete<TOption,TValue>` | Yes (`OnOptionsSearch`, async/debounced) | `MaxAutoHeight`, `MaximumOptionsSearch` (default 9) | Recommended for very large lists; `Multiple="false"` + `@bind-SelectedItem`; docs note **accessibility not yet implemented**. |

## 4. Decision

**Adopting Fluent UI is a UI change to a locked design system.** It will (a) look different from the
Telemetry Slate prototypes and (b) require the `e06s05` parity probes to be rewritten. It also risks the
strict CSP because Fluent components emit inline `style` attributes (e.g. `Height` → `max-height`,
`MaxAutoHeight` → CSS variable) which `style-src-attr 'none'` blocks.

Two viable paths:

- **A — Fluent UI Blazor (requested).** Highest convenience; needs an ADR, package, CSP spike, and parity-gate update.
- **B — Native `<input list>` + `<datalist>` (no dependency).** Built-in type-to-filter and browser-capped scroll; keeps the Tailwind design and CSP untouched; no new package; but popup styling is browser-controlled and cannot be a fixed Fluent panel.

**Recommendation:** path **A**, with a short spike first to confirm the CSP and visual impact, then the plan below. Recommended components:
`FluentCombobox` (single-select, immediate filter) for **country**, and the same for **city** (list ≤ 5, still consistent).
Use `FluentAutocomplete` instead only if we decide to move filtering server-side later.

## 5. Implementation plan (path A)

1. **ADR + design update.** New `specs/adr/00xx-searchable-location-select-fluent-ui.md`; note the scope
   revision in `UI_DESIGN_PLAN_LATEST.md` (D1/D4 apply to the rest of the UI; the query panel opts into Fluent).
2. **Dependency.** Add `Microsoft.FluentUI.AspNetCore.Components` to `Directory.Packages.props`, reference it
   in `Weather.WebUI.csproj`.
3. **Registration.** `AddFluentUIComponents()` in `WebUiServiceCollectionExtensions.AddWeatherUi`.
4. **Component swap** in `Components/Explorer/QueryControllerPanel.razor`:
   - `FluentCombobox<CountryModel,string>` (country) and `FluentCombobox<CityModel,string>` (city),
     `Multiple="false"`, `Immediate="true"`, `@bind-ImmediateText` → filter `Items` in the panel,
     `Height="10rem"` (≈10 rows) to cap and scroll, `Label`/`Placeholder`.
   - Keep `OnCountryChanged` / `OnCityChanged` parameters; fire them from the value-changed callback.
   - Keep loading/disabled behaviour for the Fetch button.
5. **Config-driven defaults** (`Location` section):
   - New `Weather.WebUI/Configuration/LocationOptions.cs` following the `CspOptions`/`DatabaseOptions` pattern:
     `SectionName = "Location"`, `DefaultCountryCode = "ID"`, `DefaultCityName = "Jakarta"`.
   - Bind in `WebUiServiceCollectionExtensions.AddWeatherUi`:
     `services.Configure<LocationOptions>(configuration.GetSection(LocationOptions.SectionName));`
   - Inject `IOptionsSnapshot<LocationOptions>` (scoped, so an `appsettings` reload is picked up on the next
     page load) into `WeatherExplorerModel`.
   - `InitializeAsync`: choose the country whose `Code` matches `DefaultCountryCode` (case-insensitive), else the
     first country. When the loaded country is the default country, choose the city whose `Name` matches
     `DefaultCityName` (case-insensitive), else the first city. Manual country changes keep the existing
     first-city behaviour.
   - Add the defaults to `appsettings.json` (blank/invalid values fall back to the first country/city):
     ```json
     "Location": { "DefaultCountryCode": "ID", "DefaultCityName": "Jakarta" }
     ```
   - Overridable per environment via `Location__DefaultCountryCode` / `Location__DefaultCityName`
     (App Service configuration / Key Vault reference), matching the project's existing config approach.
6. **Client (optional, if server filtering chosen).** Add `string? search` to `WeatherApiClient.GetCountriesAsync`
   and call `api/countries?search={term}` from `FluentAutocomplete.OnOptionsSearch`. **Not** needed for path A's
   in-memory filter over 249 rows.

## 6. Test plan

- **Unit** (`Weather.WebUI.UnitTests/Services/WeatherExplorerModelTests.cs`) — construct the model with
  `Options.Create(new LocationOptions { … })`:
  - initialises to the configured country + city when both exist (default `ID`/`Jakarta`);
  - falls back to the first country / first city when the configured values are absent or blank;
  - honours a different configured pair (proves it is not hard-coded);
  - changing country still loads its cities + report (existing test stays green).
- **Config binding** — a `LocationOptions` test (mirroring `DatabaseConfigurationTests`) that binds the
  `Location` section and asserts the defaults + overrides.
- **Integration** (`Weather.IntegrationTests/Http/WeatherExplorerPageTests.cs`)
  - page renders the new picker markup; keep the existing static assertions.
- **Playwright** (`tests/visual/parity.spec.ts`)
  - replace the `select` probes (or target the new element) and keep the "no CSP violations" assertion;
  - add a filter assertion: typing `Ind` surfaces `Indonesia`.
- **CSP** — covered by the parity collector; add a unit assertion in `CspPolicyTests` only if the policy changes.
- **Gates** — Preflight (`dotnet build --warnaserror && dotnet format --verify-no-changes && dotnet test`),
  `npm run parity:styles`, `scripts/check-layout.sh`, `scripts/check-tailwind-css.sh`.

## 7. Risks

| Risk | Mitigation |
|------|------------|
| Fluent inline `style` violates `style-src-attr 'none'` | **Spike first.** If unavoidable, prefer a non-inline configuration; relaxing the CSP is a last resort requiring review. |
| Visual divergence + parity gate break | Update probes intentionally; record the change in the ADR/verification. |
| `FluentAutocomplete` accessibility gap | Prefer `FluentCombobox` (single-select). |
| New dependency (size, supply chain) | MIT, Microsoft-published; pin centrally. |

## 8. Out of scope

- Server-side city search (list is ≤ 5).
- Changing the Geography API or seed data.
- Restyling the rest of the app with Fluent UI.

## 9. Task list

Every task ships a runnable `verify:` and a risk (`P0`–`P3`). Order matters; each step leaves the build green.
Workstream A is independent of the picker decision and can land first. Workstream B is the go/no-go for
Fluent UI; C is the swap; D is evidence.

### Workstream A — Config-driven default location

| ID | Task | verify | risk |
|----|------|--------|------|
| A1 | Add `Configuration/LocationOptions.cs` (`SectionName = "Location"`, defaults `ID`/`Jakarta`) and register `services.Configure<LocationOptions>(configuration.GetSection(...))` in `AddWeatherUi`. | `dotnet build Weather.sln --warnaserror` | P1 |
| A2 | Add the `Location` block to `appsettings.json`; document `Location__DefaultCountryCode` / `Location__DefaultCityName` overrides in the plan/README. | `dotnet test Weather.sln --filter Category=DatabaseConfiguration` | P2 |
| A3 | Inject `IOptionsSnapshot<LocationOptions>` into `WeatherExplorerModel`; resolve default country (case-insensitive; blank/unknown → first) and default city (only for the default country; blank/unknown → first). Update the existing test constructor. | `dotnet test Weather.sln --filter Category=WeatherExplorerModel` | P0 |
| A4 | Add unit tests (defaults honoured; blank/unknown fallback; a different configured pair honoured) plus a `LocationOptions` binding test mirroring `DatabaseConfigurationTests`. | `dotnet test Weather.sln --filter "Category=WeatherExplorerModel|Category=LocationOptions"` | P0 |
| A5 | Workstream gate. | `dotnet build Weather.sln --warnaserror && dotnet format Weather.sln --verify-no-changes && dotnet test Weather.sln` | P0 |

### Workstream B — Fluent UI spike (go/no-go)

| ID | Task | verify | risk |
|----|------|--------|------|
| B1 | Pin `Microsoft.FluentUI.AspNetCore.Components` (5.0.1; conservative fallback 4.14.5) in `Directory.Packages.props`, reference it in `Weather.WebUI.csproj`, call `AddFluentUIComponents()`. `[OK]` MIT, Microsoft-published. | `dotnet build Weather.sln --warnaserror` | P1 |
| B2 | Render one `FluentCombobox` in the real query panel under the enforced CSP and capture the console: does it load under `script-src 'self'`? do its inline styles violate `style-src-attr 'none'`? does the `Height`-capped, filtered list work? | `npm --prefix src/Weather.WebUI run parity:styles` (asserts zero CSP violations) + inspect the artifact | P0 |
| B3 | Record go/no-go: if no violations → continue path A; else switch pickers to native `<input list>` + `<datalist>` (path B) and continue. Write `specs/adr/00xx-searchable-location-select-fluent-ui.md` with the evidence. | `test -f specs/adr/00xx-searchable-location-select-fluent-ui.md` | P0 |

### Workstream C — Searchable pickers

| ID | Task | verify | risk |
|----|------|--------|------|
| C1 | Replace the country `<select>` in `QueryControllerPanel.razor` with the chosen component (path A: `FluentCombobox` immediate filter over `Countries`, ~10-row cap; path B: `<input list>` + `<datalist>`), preserving `OnCountryChanged`. | `dotnet build Weather.sln --warnaserror && dotnet test Weather.sln --filter Category=WeatherExplorerPage` | P1 |
| C2 | Replace the city `<select>` the same way over `Cities`. | `dotnet build Weather.sln --warnaserror && dotnet test Weather.sln --filter Category=WeatherExplorerPage` | P1 |
| C3 | Update `tests/visual/parity.spec.ts`: retarget the `select` probes to the new control, keep the zero-CSP assertion, add a filter check (type `Ind` → `Indonesia`). | `npm --prefix src/Weather.WebUI run parity:styles` | P0 |
| C4 | Rebuild and commit the compiled Tailwind CSS if new classes were added. | `bash scripts/check-tailwind-css.sh` | P2 |
| C5 | Workstream gate. | `dotnet build Weather.sln --warnaserror && dotnet format Weather.sln --verify-no-changes && dotnet test Weather.sln && npm --prefix src/Weather.WebUI run parity:styles` | P0 |

### Workstream D — Verification & evidence

| ID | Task | verify | risk |
|----|------|--------|------|
| D1 | Full preflight plus the layout guard. | `bash scripts/check-layout.sh && dotnet build Weather.sln --warnaserror && dotnet format Weather.sln --verify-no-changes && dotnet test Weather.sln` | P0 |
| D2 | Write `specs/verifications/<story>.md` with command output and before/after screenshots. | `test -f specs/verifications/<story>.md` | P2 |

Bookkeeping once approved: turn this into a story capsule via `change-request` / `plan-work`
(`specs/epics/<eNN>/` + `-tasks.yaml`), register it in `release-plan.yaml`, then `kickoff-branch` → `develop-tdd`.
