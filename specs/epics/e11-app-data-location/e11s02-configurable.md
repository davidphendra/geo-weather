# Story e11s02 — Source the SQLite data location from `appsettings.json`

| Field | Value |
|-------|-------|
| ID | e11s02 |
| Epic | e11 SQLite Data File Location |
| Status | todo |
| BCPs | 3 |
| Risk | P2 |
| Type | refactor |
| ADR | ADR-0013 (amended) |

## 1. Summary
Remove the hardcoded `App_Data` folder name and per-module SQLite file names from `Program.cs` and source them
from a configurable `Database` section in `appsettings.json`.

## 2. Context
`e11s01` moved SQLite files into `App_Data` and made it non-servable, but the host hardcoded the directory and
the three file names. Configuration belongs in configuration so the data location can change per environment
(dev volume, container mount, Azure App Service `%HOME%`) without a code change.

## Requirements (delta)

### MODIFIED — SQLite data location
**Before:** `Program.cs` hardcodes `"App_Data"` and a `Dictionary` of `Geography/Favorites/Notes` → file names,
then injects `ConnectionStrings:*` defaults in memory.
**After:** `appsettings.json` declares the directory and file names; the host binds them (`Database` section),
resolves the directory against the content root (or accepts an absolute path), and derives the
`ConnectionStrings:*` defaults. No module names or file names appear in host code.

## 3. User story
As an operator, I want the SQLite data directory and file names in `appsettings.json`, so I can relocate or
rename them per environment without editing code.

## 4. Business value
Environment-portable storage location; no recompile to change where data is persisted.

## 5. In scope
- New `Database` section in `appsettings.json`:
  ```json
  "Database": {
    "DataDirectory": "App_Data",
    "Files": {
      "Geography": "geography.db",
      "Favorites": "favorites.db",
      "Notes": "notes.db"
    }
  }
  ```
- A `DatabaseOptions` configuration type (host) with `DataDirectory` + `Files`.
- `Program.cs` binds the section, resolves `DataDirectory` (absolute or content-root-relative), and composes the
  `ConnectionStrings:*` defaults only when a connection string is not already configured.
- Delete the hardcoded `sqliteFileNameByConnection` dictionary.
- Tests: overriding `Database:DataDirectory`/`Files` relocates the databases; the not-served guard still passes.

## 6. Out of scope
- Moving the location decision to a shared `BuildingBlocks` options type.
- Reading `appsettings.json` from the EF design-time factories (they remain dev-only tooling with a local
  default; noted, not changed here).
- Any change to the database provider or file format.

## 7. Domain concepts
None.

## 8. Layer touchpoints
| Assembly | Change |
|----------|--------|
| `Weather.WebUI` | `appsettings.json`, new `Configuration/DatabaseOptions.cs`, `Program.cs` |
| `Weather.IntegrationTests` | new `DatabaseConfiguration` test |

## 9. API contract
Unchanged.

## 10. Dependencies
e11s01.

## 11. Risks
| Risk | Mitigation |
|------|-----------|
| Missing/empty config silently falls back to module defaults | Guard: only compose defaults when `DataDirectory` and `Files` are non-empty; document |
| Binding a dictionary via `Get<T>` is fragile | Use a class with settable properties and a `Dictionary<string,string>` |
| Relative path in a non-content-root working directory | Resolve non-rooted `DataDirectory` against `ContentRootPath` |
| Existing `ConnectionStrings` override precedence changes | Compose defaults only when `GetConnectionString(name)` is null/empty (unchanged behaviour) |

## 12. Implementation steps
1. Add the `Database` section to `appsettings.json` → verify: `grep -q '"Database"' src/Weather.WebUI/appsettings.json`
2. Add `Configuration/DatabaseOptions.cs` → verify: `dotnet build src/Weather.WebUI/Weather.WebUI.csproj --warnaserror`
3. Refactor `Program.cs` to bind and compose, deleting the hardcoded dictionary → verify: `dotnet build src/Weather.WebUI/Weather.WebUI.csproj --warnaserror`
4. Add `DatabaseConfiguration` tests (relocated directory used; not-served guard still passes) → verify: `dotnet test Weather.sln --filter Category=DatabaseConfiguration`
5. Amend ADR-0013 + tech-stack + CONVENTIONS → verify: `grep -q 'Database:DataDirectory' specs/adr/0013-sqlite-app-data-location.md`
6. Full preflight → verify: `dotnet build Weather.sln --warnaserror && dotnet format Weather.sln --verify-no-changes && dotnet test Weather.sln`

## 13. Test plan
- `ConfigurableDataDirectory_IsUsed`: custom `WebApplicationFactory` sets `Database:DataDirectory` to a temp dir
  and omits `ConnectionStrings`; after first request, `geography.db`/`favorites.db`/`notes.db` exist under it.
- `DatabaseFiles_AreNotServedOverHttp` (from e11s01) remains green.
- Existing 177 tests remain green.

## 14. Acceptance criteria (Gherkin)
```gherkin
Scenario: Data directory comes from configuration
  Given appsettings.json sets Database:DataDirectory to a temp path
  When the host starts
  Then the SQLite files are created under that path

Scenario: Connection strings still win
  Given ConnectionStrings:Geography is configured
  Then Database:Files is not used for Geography

Scenario: No hardcoded names in the host
  When I inspect Program.cs
  Then it contains no "geography.db" / "favorites.db" / "notes.db" literal
```

## 15. Definition of done
Preflight green; host contains no hardcoded DB names/directory; config test green; ADR-0013 amended.

## 16. Verification script
1. `dotnet test Weather.sln --filter Category=DatabaseConfiguration`
2. `grep -n 'geography.db' src/Weather.WebUI/Program.cs` → no match.

## 17. Out of scope
Section 6.

## 18. Risks / notes
The `Files` keys must match the `ConnectionStrings` keys the modules read. A mismatch means that module falls
back to its own default.

## 19. Sizing / BCP rationale
3 BCPs — config section + options type + host refactor + test + docs.

## 20. References
- `specs/adr/0013-sqlite-app-data-location.md`
- Configuration in .NET: https://learn.microsoft.com/dotnet/core/extensions/configuration
