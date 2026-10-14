# Story e02s01 — Geography: Country catalog + EF migrations + GeographyController + OpenAPI

| Field | Value |
|-------|-------|
| ID | e02s01 |
| Epic | e02 Geography Module (M1) |
| Status | todo |
| BCPs | 6 |
| Risk | P1 |
| Type | feat |

## 1. Summary
Stand up the Geography module read foundation: the `Country` domain type and `CountryCode` value object; an EF
Core `GeographyContext` (`geography.db`) created by **migrations**, with the full ISO-3166 catalog seeded by a
**seed migration script**; a SQL-filtered `GetCountries` query exposed through `IGeographyService`; and a
controller-based `GeographyController` (`GET /api/countries?search=`). Also establish the host's controller base
+ OpenAPI wiring, migrate the Weather endpoint to a controller, and add controller unit tests.

## 2. Context
Geography is the supporting bounded context owning the country/city catalog. Self-contained per ADR-0008;
controller-based APIs per ADR-0009. This story delivers the country read, the migration/seeding mechanism, and the
host presentation foundation used by later modules.

## 3. User story
As a user, I want the list of supported countries (optionally filtered), so I can choose where to look up weather.

## 4. Business value
Unlocks the country dropdown, establishes migrations + seeding, and puts a consistent controller-based API in place.

## 5. In scope
- `CountryCode` value object (ISO-3166 alpha-2, upper-case, guard) and `Country` (`Code`, `Name`, `Create`).
- `GeographyContext` + `CountryEntityTypeConfiguration` + design-time factory; registered in `AddGeographyModule`.
- `CreateGeographySchema` migration (Countries) + `SeedCountries` migration (SQL, **full ISO-3166**, `Down` deletes).
- `CountryQuery(string? SearchTerm)`, `ICountryReadRepository` (`GetAsync`, `ExistsAsync` — **no `GetAllAsync`**),
  `GetCountriesQuery` + handler (`.AsNoTracking()`, `EF.Functions.Like`, `OrderBy` on `IQueryable`).
- `CountryDto` + mapper.
- `IGeographyService.GetCountriesAsync(string? search, ct)` + `GeographyService` + `AddGeographyModule`.
- `ApiControllerBase` (`ControllerBase`, `[ApiController]`, `[Produces]`).
- `GeographyController` (`[Route("api/countries")]`, `ActionResult<T>`, `[FromQuery]`, `[ProducesResponseType]`).
- OpenAPI: `AddOpenApi()` + `MapOpenApi()` (Swagger UI in Development).
- Align Weather naming with the `I<Module>Service` convention: provider port `IWeatherService` → `IWeatherProvider`; module API `IWeatherModule` → `IWeatherService`; impl `WeatherModule` → `WeatherService`.
- Migrate `WeatherEndpoints` → `WeatherController` (same routes).
- New `Weather.Api.UnitTests` project with controller unit tests.

## 6. Out of scope
Cities (e02s02); integration event (e02s03, deferred); BuildingBlocks; other modules.

## 7. Domain concepts
`Country`, `CountryCode`.

## 8. Layer touchpoints
| Assembly | Change |
|----------|--------|
| `Geography.Domain` | `Countries/Country`, `Countries/CountryCode` |
| `Geography.Application` | `Contracts/IGeographyService`, `Countries/GetCountries` (+ `CountryQuery`), `Abstractions/ICountryReadRepository`, `Configuration/ValidationBehavior` |
| `Geography.Infrastructure` | `GeographyContext`, `Configurations/CountryEntityTypeConfiguration`, `Migrations/*` (schema + seed), `Repositories/CountryReadRepository`, `GeographyService`, `GeographyModuleExtensions` |
| `Weather.Api` | `Common/ApiControllerBase`, `Modules/Geography/GeographyController`, `Modules/Weather/WeatherController`, `Program` (controllers + OpenAPI) |
| `Weather.Api.UnitTests` | controller unit tests |

## 9. API contract
- `GET /api/countries` → `200` full catalog
- `GET /api/countries?search=united` → `200` filtered in SQL (`Name LIKE '%united%'`)
- `GET /api/countries?search=zzz` → `200` `[]`
- OpenAPI document at `/openapi/v1.json`; Swagger UI in Development.

## 10. Dependencies
e00s01 (module scaffold). Weather module (for the controller migration). No BuildingBlocks.

## 11. Risks
| Risk | Mitigation |
|------|-----------|
| Seed migration SQL is provider-specific | SQLite is the only provider (ADR-0006); documented |
| Tests miss the seed because `EnsureCreated` skips migrations | tests call `Database.Migrate()` |
| Migration SQL is large (~250 rows) | keep inserts in an embedded `.sql` resource, not inline C# |
| Filtering leaks into memory | repository-only `IQueryable`; a query-shape assertion in tests |
| `[ApiController]` 400 vs FluentValidation 400 | both produce `ProblemDetails`; FluentValidation stays in the pipeline |

## 12. Implementation steps
1. Add packages centrally (EF Core Sqlite, EF Core Design, MediatR, FluentValidation) + references → verify: `dotnet restore Weather.sln`
2. Add `CountryCode` + `Country` → verify: `dotnet test src/Modules/Geography/Tests/Weather.Modules.Geography.Tests --filter Category=CountryDomain`
3. Add `GeographyContext` + `CountryEntityTypeConfiguration` + design-time factory → verify: `dotnet build …Geography.Infrastructure.csproj --warnaserror`
4. Add `CreateGeographySchema` + `SeedCountries` (SQL) migrations → verify: `dotnet test … --filter Category=GeographyMigrations`
5. Add `CountryQuery` + `ICountryReadRepository` + `GetCountriesQuery` + handler (SQL filter) → verify: `… --filter Category=GetCountries`
6. Add `CountryDto` + mapper → verify: `… --filter Category=CountryDto`
7. Add `IGeographyService` + `GeographyService` + `AddGeographyModule` → verify: `dotnet build …Geography.Infrastructure.csproj --warnaserror`
8. Add `ApiControllerBase` + `GeographyController` + `AddControllers()`/`MapControllers()` → verify: `dotnet test src/Tests/Weather.IntegrationTests --filter Category=GeographyEndpoint`
9. Add OpenAPI (`AddOpenApi()`/`MapOpenApi()`) → verify: `dotnet test src/Tests/Weather.IntegrationTests --filter Category=OpenApi`
10. Align Weather naming (`IWeatherService`→`IWeatherProvider`, `IWeatherModule`→`IWeatherService`, `WeatherModule`→`WeatherService`) → verify: `dotnet test Weather.sln`
11. Migrate `WeatherEndpoints` → `WeatherController` → verify: `dotnet test src/Tests/Weather.IntegrationTests --filter Category=WeatherEndpoint`
12. Add `Weather.Api.UnitTests` + controller unit tests → verify: `dotnet test src/Tests/Weather.Api.UnitTests`
13. Standards gate → verify: `dotnet format src/Modules/Geography/Weather.Modules.Geography.Infrastructure/… --verify-no-changes`

## 13. Test plan
- Domain: `CountryCode` normalization/guard; `Country.Create` guards.
- Migration: `Migrate()` on `:memory:` creates `Countries` and seeds the full catalog; `Down` removes seed rows.
- Application: handler passes the `CountryQuery` through; DTO mapping.
- Repository (integration): `search` filters in SQL; no-match empty; `ExistsAsync` true/false.
- Controller unit: `Ok` with the stub list.
- Host: `GET /api/countries` returns seeded countries; `?search=` filters.

## 14. Acceptance criteria (Gherkin)
```gherkin
Scenario: List countries
  Given the Geography migrations have run
  When I GET /api/countries
  Then the response is 200
  And it contains "GB" and "US"

Scenario: Filter in SQL
  When I GET /api/countries?search=united
  Then the response contains "GB" and not "JP"

Scenario: OpenAPI is exposed
  When I GET /openapi/v1.json
  Then the document describes GET /api/countries
```

## 15. Definition of done
Tasks green; migrations create + seed the catalog; controller returns `200`; OpenAPI exposed; Domain references BCL only.

## 16. Verification script
1. `dotnet run --project src/Weather.Api` → `curl -s "localhost:5000/api/countries?search=united"` and `/openapi/v1.json`.
2. `dotnet test Weather.sln`.

## 17. Out of scope
Cities, event, BuildingBlocks.

## 18. Risks / notes
Self-contained: reads through the module's own repository port; controller only calls `IGeographyService`.

## 19. Sizing / BCP rationale
6 BCPs — aggregate + migrations/seeding + query + contract + controller/OpenAPI foundation + Weather naming/migration.

## 20. References
- `specs/adr/0008-geography-module-self-contained.md`
- `specs/adr/0009-controller-based-apis.md`
