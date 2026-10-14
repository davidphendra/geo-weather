# Story e11s04 — Create the SQLite directory for any configured connection string

| Field | Value |
|-------|-------|
| ID | e11s04 |
| Epic | e11 SQLite Data File Location |
| Status | todo |
| BCPs | 1 |
| Risk | P1 |
| Type | fix |
| ADR | ADR-0013 (amended) |

## 1. Summary
Fix a startup crash: when a `ConnectionStrings` entry points at a directory that does not exist yet, the app
threw `SQLite Error 14: 'unable to open database file'` before serving any request. Create the parent directory
of every resolved SQLite connection string, not only the default `App_Data` path.

## 2. Context
`e11s02` created the directory only for the default path it composed from the `Database` section. CI/CD
deployments commonly override `ConnectionStrings__<Module>` (for example to a mounted volume) — those
directories were never created, so migrations failed at startup. Reproduced on a published artifact:
`ConnectionStrings__Geography=Data Source=/tmp/weather-nonexistent/nested/geography.db` →
`SqliteException … 'unable to open database file'`, host never started.

This does **not** mean the databases must be shipped: with no override the app creates both the directory and
the databases at runtime (verified). The defect is limited to configured paths.

## Requirements (delta)

### MODIFIED — data directory provisioning
**Before:** only the directory derived from the `Database` section (default path) was created; an explicit
`ConnectionStrings` path with a missing directory crashed startup.
**After:** the host creates the parent directory of every resolved SQLite connection string (default or
override) before the module initializers run `Database.Migrate()`.

## 3. User story
As a deployer, I want the app to prepare the configured data directory, so a fresh environment starts without a
manual `mkdir` or a startup crash.

## 4. Business value
Removes a class of CI/CD startup failures without shipping database files.

## 5. In scope
- `Weather.WebUI/Configuration/SqliteConnectionDirectory.EnsureCreated` (parses the connection string, skips
  `:memory:`, creates the parent directory).
- Host wiring: iterate `ConnectionStrings` and ensure each directory exists.
- Regression test with a nested, non-existent directory via `ConnectionStrings__Favorites`.
- ADR-0013 amendment.

## 6. Out of scope
- Shipping seed databases.
- Non-SQLite providers.

## 7. Domain concepts
None.

## 8. Layer touchpoints
| Assembly | Change |
|----------|--------|
| `Weather.WebUI` | `Configuration/SqliteConnectionDirectory.cs`, `Program.cs` |
| `Weather.IntegrationTests` | `DatabaseConfigurationTests.ConnectionStringDirectory_IsCreatedWhenMissing` |

## 9. API contract
Unchanged.

## 10. Dependencies
e11s02.

## 11. Risks
| Risk | Mitigation |
|------|-----------|
| Non-SQLite connection string parsed as SQLite | Narrow `catch (ArgumentException)` skips it |
| `:memory:` treated as a file | Explicit skip for `:memory:` and `Mode=Memory` |
| Relative path resolved against the wrong base | `Path.GetFullPath` matches how Microsoft.Data.Sqlite resolves it |

## 12. Implementation steps
1. Add `SqliteConnectionDirectory` helper → verify: `dotnet build src/Weather.WebUI/Weather.WebUI.csproj --warnaserror`
2. Wire host to ensure every configured connection-string directory → verify: `dotnet test Weather.sln --filter Category=DatabaseConfiguration`
3. Amend ADR-0013 → verify: `grep -q 'parent directory' specs/adr/0013-sqlite-app-data-location.md`
4. Full preflight → verify: `dotnet build Weather.sln --warnaserror && dotnet format Weather.sln --verify-no-changes && dotnet test Weather.sln`

## 13. Test plan
- `ConnectionStringDirectory_IsCreatedWhenMissing`: nested directory via override → app serves `200` and the file exists.
- Manual: published host with `ConnectionStrings__Geography` to a non-existent nested dir starts and creates it.

## 14. Acceptance criteria (Gherkin)
```gherkin
Scenario: Configured directory does not exist
  Given ConnectionStrings__Geography points at a missing nested directory
  When the host starts
  Then the directory is created
  And the app serves /api/countries with 200
```

## 15. Definition of done
Reproduction no longer fails; regression test green; ADR-0013 documents directory provisioning; preflight green.

## 16. Verification script
1. `ConnectionStrings__Geography="Data Source=/tmp/x/y/geography.db" dotnet run --project src/Weather.WebUI` → starts, `/tmp/x/y/geography.db` created.

## 17. Out of scope
Section 6.

## 18. Risks / notes
The directory must still be writable at runtime; the app provisions it but cannot make an unwritable volume writable.

## 19. Sizing / BCP rationale
1 BCP — one helper + host wiring + one test.

## 20. References
- `specs/adr/0013-sqlite-app-data-location.md`
- Microsoft.Data.Sqlite: "SQLite creates the database file … the directory must exist."
