# Story e15s01 — Report coordinates (Coordinates value object → OpenWeather `coord` → WeatherReportDto)

| Field | Value |
|-------|-------|
| ID | e15s01 |
| Epic | e15 City Map on the Weather Page (M2, core) |
| Status | planned |
| BCPs | 5 |
| Risk | P0 |
| Type | feat |
| Delta | ADDED |

## 1. Summary
Add a Weather-domain `Coordinates` value object and carry the provider's `coord` block on `WeatherReport`;
expose it as flat `latitude`/`longitude` on `WeatherReportDto` so the API and the WebUI can locate the
observed city. The forecast contract is unchanged (ADR-0018 D1).

## 2. Context
`OpenWeatherMapResponse` currently binds `name`, `dt`, `visibility`, `main`, `wind`, `sys`, `weather`
but not `coord`. `WeatherReport.Create` takes no coordinates. Per the OpenWeather Current Weather 2.5
docs, the payload includes `coord: { "lon": …, "lat": … }` in decimal degrees. Domain derivation
belongs in the Weather domain (ADR-0003), so the WGS-84 pair is modelled as a value object with range
guards, not as loose doubles.

## 3. User story
As a user, I want the weather report to tell me exactly where the observation applies, so the page can
draw the city on a map.

## 4. Business value
Supplies the coordinates the map needs, from the data the app already fetches — no extra provider call,
no API key, offline-testable.

## 5. In scope
- Domain `Coordinates` value object (`Latitude`, `Longitude`, `Of(lat, lon)` with range guards throwing `WeatherDomainException`).
- `WeatherReport` gains a `Coordinates` property; `WeatherReport.Create` requires it.
- `OpenWeatherMapResponse` binds `coord` (`lat`, `lon`); `OpenWeatherMapMapper` passes it through.
- `WeatherReportDto` gains `decimal Latitude, decimal Longitude` at the top level; `WeatherReportDtoMapper` maps them.
- Update every `WeatherReport.Create` call site and canned-JSON fixture.
- Domain, mapping, and DTO tests.

## 6. Out of scope
Forecast coordinates (forecast `LocationDto`/adapter/DTO unchanged); the WebUI `WeatherReportModel`,
`StationMap`, Leaflet, tiles, and CSP (e15s02–e15s04); any new endpoint.

## 7. Domain concepts
`Coordinates` (new), `WeatherReport`, `Location` (unchanged).

## 8. Layer touchpoints
| Assembly | Change |
|----------|--------|
| Weather.Modules.Weather.Domain | `WeatherReports/Coordinates.cs` (new); `WeatherReport.cs` |
| Weather.Modules.Weather.Infrastructure | `OpenWeatherMap/OpenWeatherMapResponse.cs`; `OpenWeatherMapMapper.cs` |
| Weather.Modules.Weather.Application | `GetCurrentWeather/WeatherReportDto.cs`; `WeatherReportDtoMapper.cs` |
| Weather.IntegrationTests | `Support/StubWeatherProvider.cs` |

## 9. API contract (changed)
`GET /api/weather/{cityName}` now returns, in addition to the existing fields:
```json
{ "location": { "city": "London", "country": "GB" },
  "latitude": 51.5074, "longitude": -0.1278, … }
```
Additive; existing consumers are unaffected. `GET /api/weather/{cityName}/forecast` is unchanged.

## 10. Dependencies
None (first story of e15).

## 11. Risks
| Risk | Mitigation |
|------|-----------|
| `WeatherReport.Create` signature change ripples through fixtures | Enumerated call sites updated in the same task; verified by the full Weather test category |
| Provider omits `coord` | Domain requires the value; a malformed payload becomes `WeatherProviderException` → `502`, matching existing malformed-body handling |
| Latitude/longitude out of range | `Coordinates.Of` throws `WeatherDomainException`, mapped to a provider error by the adapter, covered by a test |
| Precision loss (double → decimal) | Explicit cast at the infrastructure boundary, as today for other metrics; asserted in node-precision mapping tests |

## 12. Requirements (delta)
- **ADDED** — Report coordinates.
  **Requirement:** a weather report exposes the observation's WGS-84 `latitude`/`longitude`, validated to
  `[-90, 90]` and `[-180, 180]`, sourced from the provider `coord` block.

## 13. Implementation steps
1. Add `Coordinates` with range-guarded `Of` and unit tests (valid, latitude out of range, longitude out of range) → verify: `dotnet test Weather.sln --filter Category=Coordinates`
2. Add `Coordinates` to `WeatherReport` / `WeatherReport.Create`; update the domain and handler fixtures that construct a report → verify: `dotnet test Weather.sln --filter Category=WeatherReport`
3. Bind `coord` in `OpenWeatherMapResponse`, pass it in `OpenWeatherMapMapper`, and add `coord` to the canned current-weather JSON fixtures → verify: `dotnet test Weather.sln --filter Category=OpenWeatherMapping`
4. Add `latitude`/`longitude` to `WeatherReportDto` + mapper; update `WeatherReportDto` tests and `WeatherControllerTests` → verify: `dotnet test Weather.sln --filter "Category=WeatherReportDto|Category=WeatherController"`
5. Update the integration `StubWeatherProvider` to supply coordinates → verify: `dotnet test Weather.sln --filter Category=WeatherEndpoint`
6. Story gate: full preflight green → verify: `dotnet build Weather.sln --warnaserror && dotnet format Weather.sln --verify-no-changes && dotnet test Weather.sln`

## 14. Test plan
- Unit (`Coordinates`): accepts boundary values ±90/±180 and `0,0`; rejects latitude > 90 / < −90 and longitude > 180 / < −180 with `WeatherDomainException`.
- Unit (`OpenWeatherMapping`): the canned payload's `coord.lat`/`coord.lon` become the report's coordinates.
- Unit (`WeatherReportDto`): the DTO carries the report's coordinates.
- Integration (`WeatherEndpoint`): `GET /api/weather/London?countryCode=GB` returns numeric `latitude`/`longitude`.
- Regression: the forecast endpoint and DTO are byte-for-byte unchanged.

## 15. Acceptance criteria (Gherkin)
```gherkin
Scenario: Report coordinates come from the provider
  Given the provider returns coord lat 51.5074 and lon -0.1278 for London
  When I request the current weather for London
  Then the response contains latitude 51.5074 and longitude -0.1278

Scenario: Out-of-range coordinates are rejected
  Given a provider payload with latitude 120
  When the adapter maps it
  Then a provider error is raised and the API answers 502

Scenario: Forecast contract is unchanged
  Given the forecast endpoint is called
  Then its payload contains no top-level latitude/longitude
```

## 16. Definition of done
Coordinates flow provider → domain → DTO; additive API change; Preflight green.

## 17. Verification script
1. `dotnet run --project src/Weather.WebUI`.
2. `curl "http://localhost:5xxx/api/weather/London?countryCode=GB"` and confirm `latitude`/`longitude`.
3. Confirm the forecast payload is unchanged.

## 18. Risks / notes
`Location` is deliberately not extended; coordinates belong to the observation, not the shared location
value used by the forecast (ADR-0018 D1).

## 19. Sizing / BCP rationale
5 BCPs — value object + domain wiring + provider binding/mapping + DTO/mapper + fixtures.

## 20. References
- `specs/adr/0018-city-map-leaflet.md` (D1)
- `specs/adr/0003-weather-provider-and-dew-point.md`
- OpenWeather Current Weather 2.5: `https://openweathermap.org/current`
