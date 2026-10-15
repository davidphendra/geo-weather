# ADR-0018 — City map on the Weather page (Leaflet + OpenStreetMap)

**Status:** Accepted (D1-D10 confirmed 2026-10-15)
**Date:** 2026-10-15

## Context
The Weather Explorer page (`/`) shows current conditions (`StationHeroCard`) and the five-day
forecast (`ForecastStrip`). The page already prints `Lat`/`Lon`, sourced today from the Geography
`CityModel` (`City.Latitude` / `City.Longitude`). The user wants an interactive map of the selected
city, placed under the forecast, drawn from the coordinates that arrive with the weather data.

The Weather module currently discards the OpenWeatherMap `coord` block: `OpenWeatherMapResponse`
does not bind it and `WeatherReport`/`WeatherReportDto` carry no coordinates. The host enforces a
strict CSP Level 3 (`script-src 'self' 'nonce-…'`, `style-src 'self' https://fonts.googleapis.com`,
`img-src 'self' data:`) built in `Weather.WebUI/Security/CspPolicy.cs`.

## Provider / library facts (verified against vendor docs, 2026-10-15)
- OpenWeatherMap Current Weather 2.5 (`GET /data/2.5/weather`) returns `coord: { "lon": …, "lat": … }`
  in decimal degrees. The adapter already calls this endpoint with `units=imperial`.
- Leaflet 1.9.4 (BSD-2-Clause) quick-start: Leaflet CSS in `<head>`, Leaflet JS after it, a `<div>`
  with a fixed height, `L.map('id').setView([lat, lon], zoom)`, an OSM tile layer
  `https://tile.openstreetmap.org/{z}/{x}/{y}.png` with attribution, and `L.marker([lat, lon])`.
  Popup content is rendered as **HTML** ("don't pass untrusted user input to it directly").
- Leaflet distributes `dist/leaflet.css`, `dist/leaflet.js` (UMD) and `dist/leaflet-src.esm.js`
  (ES module) on unpkg, with published SRI hashes for the CSS/JS:
  - `leaflet.css` `sha256-p4NxAoJBhIIN+hmNHrzRCf9tD/miZyoHS5obTRR9BMY=`
  - `leaflet.js` `sha256-20nQCchB9co0qIjJZRGuk2/Z9VM+kNiyxNV1lvTlZBo=`
- OpenStreetMap's tile usage policy requires visible attribution ("© OpenStreetMap contributors").

## Decision
1. **D1 — Coordinate source (B):** bind the OpenWeatherMap `coord` block in the current-weather
   adapter and carry it through the domain as a new `Coordinates` value object on `WeatherReport`.
   Expose it as flat `latitude`/`longitude` on `WeatherReportDto`. The **forecast** contract is
   unchanged.
2. **D2 — Delivery (CDN, lazy):** load Leaflet 1.9.4 from unpkg with `integrity` + `crossorigin`,
   injected on the first `StationMap` render and removed on dispose — not loaded on other pages.
3. **D3 — Tiles (OpenStreetMap standard):** `https://tile.openstreetmap.org/{z}/{x}/{y}.png`, no API
   key, attribution shown.
4. **D4 — Content (marker + click popup):** the default Leaflet marker; clicking opens a popup with
   `City, Country — °C / °F`, built from `document.createElement` + `textContent` (never HTML).
5. **D5 — Placement:** in the left column (`lg:col-span-7`), directly under `ForecastStrip`.
6. **D6 — Missing coordinates:** render the section with an explicit
   "Map not available — coordinates only." state; the client mirror uses `decimal?` so absence is
   distinguishable from `0,0`.
7. **D7 — Version:** Leaflet 1.9.4.
8. **D8 — Single source of truth:** `StationHeroCard` reads `Report.Latitude/Longitude`, falling back
   to the `City` coordinates when the report has none.
9. **D9 — Component:** `StationMap` (`Components/Explorer/StationMap.razor` + `.razor.js`).
10. **D10 — CSP allow-list:** add the pinned unpkg origin to `script-src`/`script-src-elem` and
    `style-src`/`style-src-elem`; add unpkg (marker images) and `https://tile.openstreetmap.org`
    (tiles) to `img-src`. Nothing else widens.

## Rationale
- The map should reflect the observation the page is reporting; the provider's `coord` is the point
  the weather actually applies to. Sourcing it from the report also keeps the Weather module
  self-contained (ADR-0007) instead of coupling the map to the Geography catalog.
- Carrying coordinates only on the current report (not the shared `Location`) keeps the change small:
  `LocationDto`, the forecast DTO, and the forecast adapter are untouched.
- Lazy CDN loading keeps ~145 KB of JS off the Favorites page while still using the exact artifacts
  Leaflet publishes (SRI-pinned).
- Text-only popups avoid the documented Leaflet popup HTML-injection hazard.

## Consequences
- Positive: the design's forecast strip is complemented by a real, provider-accurate map; the hero
  card and map share one coordinate source; no new API key.
- Negative: `unpkg.com` and `tile.openstreetmap.org` become runtime dependencies and CSP origins; the
  map's browser behaviour (tiles, marker, popup, dispose) is **not** exercised by the offline dotnet
  tests (verified by manual run and the Playwright harness where practical).
- Risk: OSM tile-policy compliance at volume; unpkg availability; Leaflet's internal pane z-index vs
  the sticky header; JS-disconnected exceptions on circuit teardown.

## Alternatives rejected
- **Vendored Leaflet** under `wwwroot/lib/` (tighter CSP, fully offline): rejected by the user in
  favour of the CDN; recorded here as the tighter option if the policy is revisited.
- **Geography `CityModel` coordinates only (no backend change):** cheapest, but the map would not be
  tied to the weather observation, contradicting the request "using the coordinates from the weather
  data".
- **Coordinates on the shared domain `Location`:** makes both current and forecast carry them, but
  forces `LocationDto`, the forecast mapper, and every `Location.Of` fixture to change for no feature
  gain.
- **CARTO / keyed tile providers:** a keyed provider adds a secret to manage; CARTO is a drop-in swap
  if the map later needs to match the theme.
- **Eager global Leaflet include:** loads the library on every page.

## Confirmed decisions
| # | Decision | Choice |
|---|----------|--------|
| D1 | Coordinate source | OpenWeather `coord` → `WeatherReport` → `WeatherReportDto` (flat lat/lon); forecast untouched |
| D2 | Delivery | unpkg CDN, pinned 1.9.4, SRI + crossorigin, lazy-injected on first map render |
| D3 | Tiles | OpenStreetMap standard, attribution shown |
| D4 | Content | default marker + click popup, text-only |
| D5 | Placement | left column, directly under `ForecastStrip` |
| D6 | Missing coordinates | explicit unavailable state; nullable client mirror |
| D7 | Leaflet version | 1.9.4 |
| D8 | Hero card | switch to report coordinates with `City` fallback |
| D9 | Component | `StationMap` |
| D10 | CSP | pin unpkg in script/style + elem; unpkg + tile host in img |

## References
- `specs/adr/0003-weather-provider-and-dew-point.md`
- `specs/adr/0007-weather-module-read-only-self-contained.md`
- `specs/adr/0011-rest-api-response-contract.md`
- `specs/adr/0017-five-day-forecast.md`
- `specs/tech-architecture/UI_DESIGN_PLAN_LATEST.md` (§5 component inventory, §6 data mapping)
- Leaflet quick-start: `https://leafletjs.com/examples/quick-start/`
- OpenWeather Current Weather 2.5: `https://openweathermap.org/current`
- OSM tile usage policy: `https://operations.osmfoundation.org/policies/tiles/`
