# Story e02s01 — Geography: Country aggregate + GetCountries

| Field | Value |
|-------|-------|
| ID | e02s01 |
| Epic | e02 Geography Module (M1) |
| Status | todo |
| BCPs | 3 |
| Risk | P1 |
| Type | feat |

## 1. Summary
Add the `Country` aggregate to the Geography module and expose `GET /api/countries` through the module's
`IGeographyModule` interface, reading with `.AsNoTracking()`.

## 2. Context
Country is the top of the Geography catalog; the WebUI uses it for the dropdown.

## 3. User story
As a user, I want the list of supported countries, so I can choose one.

## 4. Business value
First bounded context; unblocks the UI and the city catalog.

## 5. In scope
- `Countries.Country` aggregate (Code, Name).
- `CountryConfiguration` + seed (GB, US, JP, AU, DE).
- `Countries/GetCountries` query + handler (AsNoTracking).
- `IGeographyModule.GetCountriesAsync` + `GeographyModule` implementation.
- `GET /api/countries` in the host's Geography area.

## 6. Out of scope
Cities (e02s02); integration event (e02s03).

## 7. Domain concepts
`Country`.

## 8. Layer touchpoints
| Assembly | Change |
|----------|--------|
| Geography.Domain | `Countries/Country` |
| Geography.Application | `Contracts/IGeographyModule`, `Countries/GetCountries` |
| Geography.Infrastructure | `GeographyContext` config + seed |
| Weather.Api | Geography area → `IGeographyModule` |

## 9. API contract
`GET /api/countries` → `200 [{ "code": "GB", "name": "United Kingdom" }]`

## 10. Dependencies
e01s02 (module template).

## 11. Risks
| Risk | Mitigation |
|------|-----------|
| Seeding idempotence | Deterministic keys |
| AsNoTracking regresses | Dedicated test |

## 12. Implementation steps
1. Add `Country` aggregate → verify: `dotnet test src/Modules/Geography/Tests --filter Category=Country`
2. Add `CountryConfiguration` + seed to `GeographyContext` → verify: `dotnet test src/Modules/Geography/Tests --filter Category=CountryConfig`
3. Add `GetCountries` query + handler (AsNoTracking) → verify: `dotnet test src/Modules/Geography/Tests --filter Category=GetCountries`
4. Implement `IGeographyModule.GetCountriesAsync` → verify: `dotnet test src/Modules/Geography/Tests --filter Category=GeographyModule`
5. Add `GET /api/countries` → verify: `dotnet test src/Tests/IntegrationTests --filter Category=Countries`

## 13. Test plan
- Unit: handler maps seeded countries.
- Integration: endpoint returns 200 + list.
- AsNoTracking: change tracker empty after read.

## 14. Acceptance criteria (Gherkin)
```gherkin
Scenario: List countries
  Given Geography is seeded
  When I GET /api/countries
  Then the response is 200 and contains "GB" and "US"
```

## 15. Definition of done
Tasks green; endpoint in OpenAPI.

## 16. Verification script
1. `dotnet run --project src/Weather.Api` → `curl localhost:5000/api/countries`.
2. `dotnet test src/Modules/Geography/Tests`.

## 17. Out of scope
Search/pagination.

## 18. Risks / notes
Country lives only in Geography; other modules keep their own projections.

## 19. Sizing / BCP rationale
3 BCPs — one aggregate + one read.

## 20. References
- `specs/adr/0005-modular-monolith-and-modules.md`
- `specs/tech-architecture/DESIGN_PLAN_LATEST.md`
