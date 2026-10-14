# Story e04s01 — Favorites: FavoriteCity aggregate + AddFavoriteCity + POST endpoint

| Field | Value |
|-------|-------|
| ID | e04s01 |
| Epic | e04 Favorites Module (M3) |
| Status | todo |
| BCPs | 4 |
| Risk | P0 |
| Type | feat |

## 1. Summary
Add the `FavoriteCity` aggregate to the Favorites module, persist it in `FavoritesContext` (`favorites.db`),
and expose `POST /api/cities/favorites` through `IFavoritesModule`, returning `201` + id and `409` on duplicate.

## 2. Context
Demonstrates a write bounded context with its own schema and its own composition root.

## 3. User story
As a user, I want to save a city to favorites, so I can find it quickly.

## 4. Business value
Fulfils the write-side requirement inside a properly owned context.

## 5. In scope
- `FavoriteCities.FavoriteCity` aggregate (Id, CityName, CountryCode, CreatedAtUtc) + uniqueness rule.
- `FavoriteCityConfiguration` with a unique index; `FavoritesContext`.
- `FavoriteCities/AddFavoriteCity` command + validator + handler.
- `IFavoritesModule.AddFavoriteAsync`.
- `POST /api/cities/favorites` → 201/409/400.

## 6. Out of scope
Listing/removing favorites.

## 7. Domain concepts
`FavoriteCity`.

## 8. Layer touchpoints
| Assembly | Change |
|----------|--------|
| Favorites.Domain | `FavoriteCities/FavoriteCity` |
| Favorites.Application | `Contracts/IFavoritesModule`, `FavoriteCities/AddFavoriteCity` |
| Favorites.Infrastructure | `FavoritesContext`, config, migration, `FavoritesModule` |
| Weather.Api | Favorites area |

## 9. API contract
`POST /api/cities/favorites { "cityName": "London", "countryCode": "GB" }` → `201 { "id": "..." }`; `409` duplicate; `400` invalid.

## 10. Dependencies
e01s02 (module template). City validation via the projection (e04s02).

## 11. Risks (security: medium — public write)
| Risk | Mitigation |
|------|-----------|
| Duplicate | Unique index + conflict mapping |
| Unknown city | Reject via the local projection |

## 12. Implementation steps
1. Add `FavoriteCity` aggregate + uniqueness rule → verify: `dotnet test src/Modules/Favorites/Tests --filter Category=FavoriteCity`
2. Add `FavoriteCityConfiguration` (unique index) + `FavoritesContext` + migration → verify: `dotnet ef migrations list --context FavoritesContext --project src/Modules/Favorites/Weather.Modules.Favorites.Infrastructure --startup-project src/Weather.Api`
3. Add `AddFavoriteCity` command + validator + handler → verify: `dotnet test src/Modules/Favorites/Tests --filter Category=AddFavorite`
4. Implement `IFavoritesModule.AddFavoriteAsync` → verify: `dotnet test src/Modules/Favorites/Tests --filter Category=FavoritesModule`
5. Add `POST /api/cities/favorites` → verify: `dotnet test src/Tests/IntegrationTests --filter Category=Favorites`
6. Assert 400/409 and that the row lands in `FavoritesContext` → verify: `dotnet test src/Tests/IntegrationTests --filter Category=FavoritesValidation`

## 13. Test plan
- Unit: handler persists (EF InMemory) and returns a Guid; validator rejects blanks.
- Integration: 201 + id; row exists in FavoritesContext; duplicate → 409; invalid → 400.

## 14. Acceptance criteria (Gherkin)
```gherkin
Scenario: Save a favorite
  Given a valid city
  When I POST /api/cities/favorites
  Then 201 and the body contains a non-empty id
  And the row exists in FavoritesContext

Scenario: Duplicate favorite
  Given London/GB is already saved
  Then a second POST returns 409
```

## 15. Definition of done
Tasks green; 201/409/400 covered; data written only in `favorites.db`.

## 16. Verification script
1. `curl -X POST localhost:5000/api/cities/favorites -d '{"cityName":"London","countryCode":"GB"}'`.
2. `dotnet test src/Modules/Favorites/Tests`.

## 17. Out of scope
Listing/removing.

## 18. Risks / notes
Uses the local City projection to reject unknown cities (cross-module asynchrony preserved).

## 19. Sizing / BCP rationale
4 BCPs — aggregate + command + own context + endpoint.

## 20. References
- `specs/adr/0006-per-module-data-isolation.md`
- `specs/tech-architecture/SECURITY_PLAN_LATEST.md`
