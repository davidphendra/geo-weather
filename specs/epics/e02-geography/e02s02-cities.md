# Story e02s02 — Geography: City aggregate + GetCitiesByCountry

| Field | Value |
|-------|-------|
| ID | e02s02 |
| Epic | e02 Geography Module (M1) |
| Status | todo |
| BCPs | 3 |
| Risk | P1 |
| Type | feat |

## 1. Summary
Add the `City` aggregate (belonging to a `Country`, with `GeoCoordinate`) and expose
`GET /api/countries/{countryCode}/cities` via `IGeographyModule`, reading with `.AsNoTracking()`.

## 2. Context
Cities are the catalog the UI browses; coordinates seed the city integration event.

## 3. User story
As a user, after choosing a country I want its cities, so I can pick one.

## 4. Business value
Completes the Geography capability; enables e02s03.

## 5. In scope
- `City` aggregate (CityId, CountryCode, Name, GeoCoordinate) + `GeoCoordinate` value object.
- `CityConfiguration` + seed (3–5 cities per country).
- `Cities/GetCitiesByCountry` query + handler (AsNoTracking) + validator.
- `IGeographyModule.GetCitiesAsync`.
- `GET /api/countries/{countryCode}/cities`; `404` unknown country.

## 6. Out of scope
City search/autocomplete.

## 7. Domain concepts
`City`, `GeoCoordinate`.

## 8. Layer touchpoints
| Assembly | Change |
|----------|--------|
| Geography.Domain | `Cities/City`, `GeoCoordinate` |
| Geography.Application | `Cities/GetCitiesByCountry`, extend `IGeographyModule` |
| Geography.Infrastructure | `CityConfiguration` + seed |
| Weather.Api | cities route |

## 9. API contract
`GET /api/countries/GB/cities` → `200 [{ "id": "...", "name": "London", "countryCode": "GB", "latitude": 51.5074, "longitude": -0.1278 }]`; `404` unknown.

## 10. Dependencies
e02s01.

## 11. Risks
| Risk | Mitigation |
|------|-----------|
| FK ordering in seed | Seed countries before cities |
| Case sensitivity | Normalize code to upper-case |

## 12. Implementation steps
1. Add `GeoCoordinate` VO with range validation → verify: `dotnet test src/Modules/Geography/Tests --filter Category=GeoCoordinate`
2. Add `City` aggregate + `CityConfiguration` + seed → verify: `dotnet test src/Modules/Geography/Tests --filter Category=CityConfig`
3. Add `GetCitiesByCountry` query + validator + handler (AsNoTracking) → verify: `dotnet test src/Modules/Geography/Tests --filter Category=GetCities`
4. Expose `IGeographyModule.GetCitiesAsync` → verify: `dotnet test src/Modules/Geography/Tests --filter Category=GeographyModule`
5. Add cities endpoint with 404 → verify: `dotnet test src/Tests/IntegrationTests --filter Category=Cities`

## 13. Test plan
- Domain: coordinate range edges.
- Unit: filter by code; unknown code → not found.
- Integration: seeded cities; 404.

## 14. Acceptance criteria (Gherkin)
```gherkin
Scenario: Cities for a known country
  When I GET /api/countries/GB/cities
  Then 200 and the list contains "London"

Scenario: Unknown country
  When I GET /api/countries/ZZ/cities
  Then 404
```

## 15. Definition of done
Tasks green; 404 covered.

## 16. Verification script
1. `curl localhost:5000/api/countries/GB/cities`.
2. `dotnet test src/Modules/Geography/Tests`.

## 17. Out of scope
City metadata beyond name + coords.

## 18. Risks / notes
Not-found semantics: unknown code → 404.

## 19. Sizing / BCP rationale
3 BCPs — second aggregate/read.

## 20. References
- `specs/tech-architecture/DESIGN_PLAN_LATEST.md`
