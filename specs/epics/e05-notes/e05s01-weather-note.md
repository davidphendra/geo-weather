# Story e05s01 — Notes: WeatherNote aggregate + AddWeatherNote + POST endpoint

| Field | Value |
|-------|-------|
| ID | e05s01 |
| Epic | e05 Notes Module (M4) |
| Status | todo |
| BCPs | 4 |
| Risk | P0 |
| Type | feat |

## 1. Summary
Add the `WeatherNote` aggregate to the Notes module, persist it in `NotesContext` (`notes.db`), and expose
`POST /api/weather/notes` through `INotesModule`, returning `201` + id.

## 2. Context
The primary write endpoint required by the assessment, implemented inside its own bounded context.

## 3. User story
As a user, I want to save a note about a city's weather, so I can record observations.

## 4. Business value
Fulfils the write-side requirement with correct data ownership.

## 5. In scope
- `Notes.WeatherNote` aggregate (Id, CityName, CountryCode, Note, TemperatureCelsius?, CreatedAtUtc) + invariants (non-empty, ≤ 500 chars).
- `WeatherNoteConfiguration` + `NotesContext`.
- `Notes/AddWeatherNote` command + validator + handler (`IDateTimeProvider`).
- `INotesModule.AddNoteAsync`.
- `POST /api/weather/notes` → 201/400.

## 6. Out of scope
Listing/editing/deleting notes.

## 7. Domain concepts
`WeatherNote`.

## 8. Layer touchpoints
| Assembly | Change |
|----------|--------|
| Notes.Domain | `Notes/WeatherNote` |
| Notes.Application | `Contracts/INotesModule`, `Notes/AddWeatherNote` |
| Notes.Infrastructure | `NotesContext`, config, migration, `NotesModule` |
| Weather.Api | Notes area |

## 9. API contract
`POST /api/weather/notes { "cityName": "London", "countryCode": "GB", "note": "Chilly", "temperatureCelsius": 12.5 }` → `201` + `Location` + `{ "id": "..." }`; `400` invalid/unknown city.

## 10. Dependencies
e01s02; city validation via projection (e05s02).

## 11. Risks (security: medium — public write)
| Risk | Mitigation |
|------|-----------|
| Unbounded text | Max length validation |
| Unknown city | Reject via projection |

## 12. Implementation steps
1. Add `WeatherNote` aggregate + invariants → verify: `dotnet test src/Modules/Notes/Tests --filter Category=WeatherNote`
2. Add `WeatherNoteConfiguration` + `NotesContext` + migration → verify: `dotnet ef migrations list --context NotesContext --project src/Modules/Notes/Weather.Modules.Notes.Infrastructure --startup-project src/Weather.Api`
3. Add `AddWeatherNote` command + validator → verify: `dotnet test src/Modules/Notes/Tests --filter Category=AddNoteValidator`
4. Add handler persisting via `NotesContext` + `IDateTimeProvider` → verify: `dotnet test src/Modules/Notes/Tests --filter Category=AddNoteHandler`
5. Implement `INotesModule.AddNoteAsync` → verify: `dotnet test src/Modules/Notes/Tests --filter Category=NotesModule`
6. Add `POST /api/weather/notes` → verify: `dotnet test src/Tests/IntegrationTests --filter Category=Notes`
7. Assert 400 + no row on invalid input → verify: `dotnet test src/Tests/IntegrationTests --filter Category=NotesValidation`

## 13. Test plan
- Unit: handler adds a row (EF InMemory) and returns a Guid; validator rejects empty/oversized.
- Integration (SQLite `:memory:`): 201 + id; row exists in NotesContext; invalid → 400, no row.

## 14. Acceptance criteria (Gherkin)
```gherkin
Scenario: Save a note
  Given a valid note payload
  When I POST /api/weather/notes
  Then 201 and the body contains a non-empty id
  And the row exists in NotesContext

Scenario: Reject an empty note
  Given an empty note
  Then 400 and no NotesContext row was created
```

## 15. Definition of done
Tasks green; 201 + persistence + 400 covered.

## 16. Verification script
1. `curl -X POST localhost:5000/api/weather/notes -d '{...}'`.
2. `dotnet test src/Modules/Notes/Tests`.

## 17. Out of scope
Note listing/management.

## 18. Risks / notes
`IDateTimeProvider` keeps `CreatedAtUtc` deterministic in tests.

## 19. Sizing / BCP rationale
4 BCPs — aggregate + command + own context + endpoint.

## 20. References
- `specs/adr/0006-per-module-data-isolation.md`
- `specs/tech-architecture/SECURITY_PLAN_LATEST.md`
