# Story e02s02 — Geography: City catalog + EF migrations + cities action

| Field | Value |
|-------|-------|
| ID | e02s02 |
| Epic | e02 Geography Module (M1) |
| Status | todo |
| BCPs | 3 |
| Risk | P1 |
| Type | feat |

## 1. Summary
Add the `City` domain type with a `GeoCoordinate` value object and a typed `CityId` (Guid), create its schema by
migration and seed a handful of major cities per country via a **seed migration script**, and expose
`GET /api/countries/{countryCode}/cities` through `IGeographyService` and `GeographyController` with a `404` for an
unknown country code. Reads use `.AsNoTracking()` and a SQL-level predicate.

## 2. Context
Cities are the catalog the WebUI browses. This completes the Geography read side (self-contained per ADR-0008;
controller-based API per ADR-0009).

## 3. User story
As a user, after choosing a country I want its cities, so I can pick one for weather.

## 4. Business value
Completes the Geography read capability and the city dropdown.

## 5. In scope
- `GeoCoordinate` (lat/long guards), `CityId` (typed Guid), `City` (`CityId`, `CountryCode`, `Name`, `GeoCoordinate`).
- `CityEntityTypeConfiguration` (FK to `Countries`, internal to Geography; `GeoCoordinate` as a complex property).
- `CreateCitiesSchema` migration + `SeedCities` migration (SQL; a handful of major cities per country; `Down` deletes).
- `ICityReadRepository` with a **SQL-filtered** `GetByCountryAsync(CountryCode, ct)`; `GetCitiesByCountryQuery` +
  validator + handler (`.AsNoTracking()`). No `GetAll`-style method.
- `CityDto` + mapper.
- `IGeographyService.GetCitiesAsync` + `GeographyService`.
- Cities action on `GeographyController` (`404` unknown country).
- Controller unit test for the cities action.

## 6. Out of scope
City search/autocomplete; integration event (e02s03, deferred).

## 7. Domain concepts
`City`, `CityId`, `GeoCoordinate`, `CountryCode`.

## 8. Layer touchpoints
| Assembly | Change |
|----------|--------|
| `Geography.Domain` | `Cities/City`, `Cities/CityId`, `GeoCoordinate` |
| `Geography.Application` | `Cities/GetCitiesByCountry`, extend `Contracts/IGeographyService`, `Abstractions/ICityReadRepository`, `Abstractions/CountryNotFoundException` |
| `Geography.Infrastructure` | `Configurations/CityEntityTypeConfiguration`, `Migrations/*` (schema + seed), `Repositories/CityReadRepository`, `GeographyService` |
| `Weather.Api` | `Modules/Geography/GeographyController` (cities action) |
| `Weather.Api.UnitTests` | cities-action controller test |

## 9. API contract
`GET /api/countries/GB/cities` → `200`
```json
[{ "id": "…", "name": "London", "countryCode": "GB", "latitude": 51.5074, "longitude": -0.1278 }]
```
`404` when the country code is unknown (via `CountryNotFoundException` → `GlobalExceptionHandler`).

## 10. Dependencies
e02s01.

## 11. Risks
| Risk | Mitigation |
|------|-----------|
| FK + seed ordering | seed countries before cities (ordered migrations) |
| Case sensitivity | normalize the code to upper-case before the query |
| Unknown vs empty | unknown code → 404; known-with-no-cities → `200 []` |
| Complex-property mapping | `GeoCoordinate` as an EF complex property; covered by a migration test |

## 12. Implementation steps
1. Add `GeoCoordinate` + `CityId` + `City` → verify: `dotnet test src/Modules/Geography/Tests/Weather.Modules.Geography.Tests --filter Category=CityDomain`
2. Add `CityEntityTypeConfiguration` + `CreateCitiesSchema` + `SeedCities` migrations → verify: `… --filter Category=CityConfig`
3. Add `ICityReadRepository` (`GetByCountryAsync`, SQL `Where`) + `GetCitiesByCountryQuery` + validator + handler → verify: `… --filter Category=GetCities`
4. Add `CityDto` + mapper → verify: `… --filter Category=CityDto`
5. Extend `IGeographyService.GetCitiesAsync` + `GeographyService` → verify: `dotnet build …Geography.Infrastructure.csproj --warnaserror`
6. Add the cities action to `GeographyController` (`404`) → verify: `dotnet test src/Tests/Weather.IntegrationTests --filter Category=GeographyErrors`
7. Controller unit test for the cities action → verify: `dotnet test src/Tests/Weather.Api.UnitTests`
8. Standards gate → verify: `dotnet format src/Modules/Geography/Weather.Modules.Geography.Infrastructure/… --verify-no-changes`

## 13. Test plan
- Domain: coordinate bounds; `CityId.From(Guid.Empty)` throws; `City.Create` guards.
- Migration: `Cities` table created + seeded; `Down` removes rows.
- Application: filter by code via the repository; unknown → `CountryNotFoundException`.
- Host: valid code returns seeded cities; unknown code → `404`.

## 14. Acceptance criteria (Gherkin)
```gherkin
Scenario: Cities for a known country
  Given the Geography migrations have run
  When I GET /api/countries/GB/cities
  Then the response is 200 and contains "London"

Scenario: Unknown country
  When I GET /api/countries/ZZ/cities
  Then the response is 404
```

## 15. Definition of done
Tasks green; `404` covered; reads use `.AsNoTracking()`; no `GetAll`-style method.

## 16. Verification script
1. `dotnet run --project src/Weather.Api` → `curl -s localhost:5000/api/countries/GB/cities`.
2. `curl -s -o /dev/null -w "%{http_code}" localhost:5000/api/countries/ZZ/cities` → `404`.

## 17. Out of scope
Search, pagination, integration events.

## 18. Risks / notes
The `City` FK to `Countries` is internal to Geography — allowed.

## 19. Sizing / BCP rationale
3 BCPs — second aggregate/read in the same module.

## 20. References
- `specs/adr/0008-geography-module-self-contained.md`
- `specs/adr/0009-controller-based-apis.md`
