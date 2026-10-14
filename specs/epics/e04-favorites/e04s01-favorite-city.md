# Story e04s01 — Favorites: FavoriteCity + AddFavoriteCity + GET + FavoritesController

| Field | Value |
|-------|-------|
| ID | e04s01 |
| Epic | e04 Favorites Module (M3) |
| Status | todo |
| BCPs | 5 |
| Risk | P0 |
| Type | feat |

## 1. Summary
Stand up the Favorites module: the `FavoriteCity` aggregate (keyed on `CityId`), its own `favorites.db`/`FavoritesContext`
created by migration, the `City` projection table (filled by e04s02), an `AddFavoriteCityCommand` (resolves the city,
rejects duplicates with `409`) and a `GetFavoritesQuery` (list joined to the projection), exposed through
`IFavoritesService` and a controller-based `FavoritesController`.

## 2. Context
Favorites is the first write module (ADR-0010). It owns its data and reads city display data from its own
projection. Self-contained persistence (ADR-0008 pattern); controller API (ADR-0009).

## 3. User story
As a user, I want to save a city to favorites (and list my favorites), so I can find it quickly.

## 4. Business value
Delivers the write-side requirement and demonstrates EF Core state mutations in an owned schema.

## 5. In scope
- `FavoriteCityId`, `FavoriteCity` (Id, CityId, CreatedAtUtc); `FavoritesDomainException`.
- `City` projection entity (CityId, CountryCode, Name, GeoCoordinate) — table created here (populated in e04s02).
- `FavoritesContext` (`favorites.db`) + configs (unique index on `FavoriteCities.CityId`; FKs internal to Favorites) + design-time factory.
- `CreateFavoritesSchema` migration.
- `IFavoriteCityRepository`, `ICityProjectionRepository`, `IUnitOfWork`, `IDateTimeProvider`.
- `AddFavoriteCityCommand` (+ validator + handler): resolve `CityId` from the projection (unknown → `CityNotFoundException` → 404); duplicate → `FavoriteAlreadyExistsException` → 409.
- `GetFavoritesQuery` (+ handler) joined to `CityProjections`.
- `FavoriteCityDto` + mapper.
- `IFavoritesService` (`AddFavoriteAsync`, `GetFavoritesAsync`) + `FavoritesService` + `AddFavoritesModule`.
- `FavoritesController` (`POST /api/cities/favorites` → 201/409; `GET /api/cities/favorites` → 200).
- Module-local `ValidationBehavior`.

## 6. Out of scope
Event consumption (e04s02); unfavorite/delete; per-user favorites; BuildingBlocks.

## 7. Domain concepts
`FavoriteCity`, `FavoriteCityId`, `City` (projection).

## 8. Layer touchpoints
| Assembly | Change |
|----------|--------|
| `Favorites.Domain` | `FavoriteCities/FavoriteCity`, `FavoriteCities/FavoriteCityId`, `Cities/City`, `GeoCoordinate`, `FavoritesDomainException` |
| `Favorites.Application` | `Contracts/IFavoritesService`, `Abstractions/*`, `FavoriteCities/AddFavoriteCity`, `FavoriteCities/GetFavorites`, `Configuration/ValidationBehavior` |
| `Favorites.Infrastructure` | `FavoritesContext`, `Configurations/*`, `Migrations/*`, `Repositories/*`, `DesignTime/*`, `FavoritesService`, `FavoritesModuleExtensions` |
| `Weather.Api` | `Modules/Favorites/FavoritesController`, `ErrorHandling/GlobalExceptionHandler` (+409/404) |

## 9. API contract
- `POST /api/cities/favorites` `{ "cityName": "London", "countryCode": "GB" }` → `201` + `Location` + `{ "id": "…" }`; `409` if already favorited; `404` unknown city; `400` invalid.
- `GET /api/cities/favorites` → `200 [ { "id", "cityId", "city", "countryCode" } ]`.

## 10. Dependencies
e00s01 (scaffold). Prerequisites for the projection data: e01s01/e01s02 (Events Bus/Outbox) and e02s03.

## 11. Risks (security: medium — public write)
| Risk | Mitigation |
|------|-----------|
| Duplicate | unique index on `CityId` + `FavoriteAlreadyExistsException` → 409 |
| Unknown city | resolve against `CityProjections` → 404 |
| Unbounded input | validator: city non-empty ≤ 100, country code length 2 |
| Race on unique index | catch DB unique violation → 409 |

## 12. Implementation steps
1. Packages (EF Core Sqlite, EF Core Design, MediatR, FluentValidation) + refs → verify: `dotnet restore Weather.sln`
2. `FavoriteCityId` + `FavoriteCity` + `FavoritesDomainException` → verify: `dotnet test src/Modules/Favorites/Tests/Weather.Modules.Favorites.Tests --filter Category=FavoriteDomain`
3. `GeoCoordinate` + `City` projection entity → verify: `… --filter Category=CityProjectionDomain`
4. `FavoritesContext` + configs + design-time factory → verify: `dotnet build …Favorites.Infrastructure.csproj --warnaserror`
5. `CreateFavoritesSchema` migration → verify: `… --filter Category=FavoritesMigrations`
6. repositories + `AddFavoriteCityCommand` + validator + handler → verify: `… --filter Category=AddFavorite`
7. `FavoriteCityDto` + `GetFavoritesQuery` + handler → verify: `… --filter Category=GetFavorites`
8. `IFavoritesService` + `FavoritesService` + `AddFavoritesModule` → verify: `dotnet build …Favorites.Infrastructure.csproj --warnaserror`
9. `FavoritesController` + `GlobalExceptionHandler` (409/404) → verify: `dotnet test src/Tests/Weather.IntegrationTests --filter Category=Favorites`
10. Standards gate → verify: `dotnet format …Favorites.Infrastructure.csproj --verify-no-changes`

## 13. Test plan
- Domain: `FavoriteCity.Create` invariants; `FavoriteCityId`.
- Application: validator rejects bad input; handler resolves `CityId` and persists (mocked repos); unknown → `CityNotFoundException`; duplicate → `FavoriteAlreadyExistsException`.
- Integration: `FavoritesContext` unique index; `AsNoTracking`.
- Host: `POST` 201; duplicate 409; unknown 404; invalid 400; `GET` 200.

## 14. Acceptance criteria (Gherkin)
```gherkin
Scenario: Save a favorite
  Given the city projection contains London/GB
  When I POST /api/cities/favorites
  Then the response is 201 with a non-empty id

Scenario: Duplicate favorite
  Given London/GB is already a favorite
  When I POST /api/cities/favorites again
  Then the response is 409

Scenario: Unknown city
  Given the projection has no "Nowhere"
  When I POST it as a favorite
  Then the response is 404
```

## 15. Definition of done
Tasks green; 201/409/404/400 covered; favorites written only in `favorites.db`.

## 16. Verification script
1. `curl -X POST localhost:5000/api/cities/favorites -d '{"cityName":"London","countryCode":"GB"}'`.
2. `curl localhost:5000/api/cities/favorites`.

## 17. Out of scope
Delete/unfavorite; per-user.

## 18. Risks / notes
Until e04s02 fills the projection, POSTs resolve to 404; tests seed the projection directly.

## 19. Sizing / BCP rationale
5 BCPs — aggregate + context/migration + command + query + contract + controller.

## 20. References
- `specs/adr/0010-favorites-module-decisions.md`
- `specs/adr/0009-controller-based-apis.md`
