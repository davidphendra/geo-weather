# Story e06s01 — Telemetry Slate design system + app shell

| Field | Value |
|-------|-------|
| ID | e06s01 |
| Epic | e06 WebUI Front-End |
| Status | todo |
| BCPs | 8 |
| Risk | P1 |
| Type | feat |

## 1. Summary
Introduce the Telemetry Slate design system (Tailwind v3.4 token build + self-authored assets) and rebuild
`App.razor` / `MainLayout` into the fixed-header / main / footer shell, replacing the Blazor template chrome.

## 2. Context
The scaffolded WebUI is the default Blazor template (sidebar, Bootstrap, `Counter`/`Weather`/`Home`). The
approved prototypes (`specs/designs/weather_explorer_asp.net_core_client/code.html`,
`specs/designs/favorites_saved_notes_ef_core_state/code.html`) are Tailwind + Geist/JetBrains Mono + Material
Symbols. This story lands the tokens, fonts, and shared chrome that every page builds on.

## 3. User story
As a user, I want the app to render in the Telemetry Slate visual identity, so the pages match the approved designs.

## 4. Business value
Without the token build and shell, no page can match the designs; this is the foundation for e06s03/e06s04.

## 5. In scope
- `tailwind.config.js` with the token block copied verbatim from the prototypes; `Styles/tailwind.css`; npm `build:css`.
- Committed `wwwroot/app.css`; additive MSBuild target so plain `dotnet build` needs no Node.
- `App.razor` head (Google fonts links + `app.css`, body classes) and body script.
- `MainLayout` = `AppHeader` + `<main>` + `AppFooter`; `AppNav`; placeholder logo `wwwroot/img/geoweather-mark.svg`.
- `.icon-fill` component-layer class (replaces the prototype's inline `style="font-variation-settings"`).
- Delete template UI: `Home.razor`, `Counter.razor`, `Weather.razor`, sidebar/NavMenu, Bootstrap assets, template `app.css`.
- Offline token-contract test in `Weather.WebUI.UnitTests`.

## 6. Out of scope
Feature pages (e06s03/e06s04); `WeatherApiClient` (e06s02); CSP (e06s06); visual-parity harness (e06s05).

## 7. Domain concepts
None. Presentation-only.

## 8. Layer touchpoints
| Assembly | Change |
|----------|--------|
| Weather.WebUI | `tailwind.config.js`, `package.json`, `Styles/tailwind.css`, `wwwroot/app.css`, `wwwroot/img/geoweather-mark.svg`, `Components/App.razor`, `Components/Layout/*` |

## 9. API contract (consumed)
None.

## 10. Dependencies
e10s01 (single host), e09s01 (response contract already landed).

## 11. Risks
| Risk | Mitigation |
|------|-----------|
| Tailwind output drifts from config | CI drift guard (`build:css` + `git diff --exit-code wwwroot/app.css`) |
| `dotnet format` may not cover `.razor` | Run Preflight after edits; fix C# only |
| Font CDN offline in dev | Local fallback stacks in the Tailwind `fontFamily` config |

## 12. Implementation steps
1. Add `src/Weather.WebUI/package.json` (`tailwindcss@3.4.17`) and the `build:css` script → verify: `npm --prefix src/Weather.WebUI install`
2. Add `tailwind.config.js` (colors/radius/spacing/fontFamily/fontSize verbatim) and `Styles/tailwind.css` → verify: `npm --prefix src/Weather.WebUI run build:css && test -s src/Weather.WebUI/wwwroot/app.css`
3. Add the `.icon-fill` class to the component layer → verify: `grep -q "icon-fill" src/Weather.WebUI/Styles/tailwind.css`
4. Commit the compiled `wwwroot/app.css` and add the additive MSBuild target → verify: `dotnet build src/Weather.WebUI/Weather.WebUI.csproj --warnaserror`
5. Add `wwwroot/img/geoweather-mark.svg` → verify: `test -f src/Weather.WebUI/wwwroot/img/geoweather-mark.svg`
6. Rebuild `App.razor` (fonts + `app.css`, body classes) and `MainLayout`/`AppHeader`/`AppNav`/`AppFooter` → verify: `dotnet build src/Weather.WebUI/Weather.WebUI.csproj --warnaserror`
7. Delete `Home.razor`, `Counter.razor`, `Weather.razor`, sidebar/NavMenu and Bootstrap assets → verify: `! grep -rqE "Counter|Hello, world|Hello, world!" src/Weather.WebUI/Components`
8. Add the token-contract test → verify: `dotnet test src/Tests/Weather.WebUI.UnitTests --filter Category=DesignTokens`

## 13. Test plan
- Token contract: compiled `app.css` contains every colour/radius/spacing/type token and a representative set of prototype utility classes.
- Manual: header/nav/footer render at 390/768/1600 px with no scrollbar overflow.

## 14. Acceptance criteria (Gherkin)
```gherkin
Scenario: Same tokens as the prototype
  Given the compiled app.css
  When I search for the design tokens
  Then primary-container #131b2e, radius DEFAULT 0.125rem, and space-xl 1.5rem are present

Scenario: Shell replaces the template
  Given the app is running
  When I open "/"
  Then I see the fixed GeoWeather header with .NET 10 badge and the footer
  And there is no sidebar
```

## 15. Definition of done
Shell renders in Telemetry Slate; template UI removed; token test green; Preflight green.

## 16. Verification script
1. `dotnet run --project src/Weather.WebUI`.
2. Open `/`; confirm fixed header, "GeoWeather" + `.NET 10`, two nav links, footer.
3. Resize to 390/768/1600 px; confirm the header stays 64px and the layout reflows.

## 17. Out of scope
Data binding; the two feature pages; CSP; the Playwright harness.

## 18. Risks / notes
Adding Node to a .NET repo is deliberate; the compiled CSS is committed so builds/tests stay Node-free.

## 19. Sizing / BCP rationale
8 BCPs — token build, shell rewrite, template removal, token test.

## 20. References
- `specs/tech-architecture/UI_DESIGN_PLAN_LATEST.md` §3, §4, §7
- `specs/designs/telemetry_slate/DESIGN.md`
