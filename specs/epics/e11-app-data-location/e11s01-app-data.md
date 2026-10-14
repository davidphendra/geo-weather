# Story e11s01 — Store SQLite files under `App_Data` (outside `wwwroot`)

| Field | Value |
|-------|-------|
| ID | e11s01 |
| Epic | e11 SQLite Data File Location |
| Status | todo |
| BCPs | 3 |
| Risk | P1 |
| Type | security |
| ADR | ADR-0013 |

## 1. Summary
Move the per-module SQLite files from the host content root into a dedicated `App_Data` folder that is never
wired to static-file middleware, and add a regression test proving the files cannot be fetched over HTTP.

## 2. Context
Defaults were `Data Source=<module>.db`, so SQLite created files in the working directory
(`src/Weather.WebUI/`). A reviewer asked whether a deployed client could download them. Microsoft Learn states
only files in `wwwroot` are addressable, so the previous layout was not an active exposure — but a dedicated
non-web-root data directory is the documented hardening pattern (see ADR-0013).

## 3. User story
As a maintainer, I want database files in a directory that is structurally outside the served web root, so that
data can never be fetched as a static asset.

## 4. Business value
Removes a class of accidental data-exposure risk and makes the storage location explicit and deployable.

## 5. In scope
- Host resolves `<content root>/App_Data` and sets default absolute connection strings when none are configured.
- Create `App_Data` before migrations run.
- EF design-time factories use the same folder.
- Regression test: sentinel `.db` under content root and `App_Data` both return `404`.
- Move existing dev DB files; `.gitignore` already ignores `App_Data/` and `*.db`.
- Docs: ADR-0013, ADR-0006 note, tech-stack, CONVENTIONS.

## 6. Out of scope
- Changing database provider or engine.
- Multi-instance/scaling strategy.
- Encrypting the SQLite file at rest.

## 7. Domain concepts
None (hosting/infrastructure).

## 8. Layer touchpoints
| Assembly | Change |
|----------|--------|
| `Weather.WebUI` | `Program.cs` App_Data resolution; `App_Data/` folder |
| Modules (Infrastructure) | design-time factories point at `App_Data/` |
| `Weather.IntegrationTests` | `SingleHostTests.DatabaseFiles_AreNotServedOverHttp` |

## 9. API contract
Unchanged.

## 10. Dependencies
e10s01 (single host).

## 11. Risks
| Risk | Mitigation |
|------|-----------|
| SQLite cannot create the parent directory | Host creates `App_Data` before `Migrate()` |
| Host default overrides a real deployment connection string | Default only applied when none is configured |
| Tests write into the source tree | Test config overrides connection strings; verified `App_Data` untouched |
| `App_Data` lost on ephemeral containers | Documented: mount persistent storage |

## 12. Implementation steps
1. Add App_Data resolution + conditional default connection strings → verify: `dotnet build src/Weather.WebUI/Weather.WebUI.csproj --warnaserror`
2. Point design-time factories at App_Data → verify: `dotnet build Weather.sln --warnaserror`
3. Move existing `.db` files into `App_Data` → verify: `ls src/Weather.WebUI/App_Data/*.db`
4. Add the not-served regression test → verify: `dotnet test Weather.sln --filter Category=SingleHost`
5. Docs → verify: `test -f specs/adr/0013-sqlite-app-data-location.md`
6. Preflight → verify: `dotnet build Weather.sln --warnaserror && dotnet format Weather.sln --verify-no-changes && dotnet test Weather.sln`

## 13. Test plan
- `DatabaseFiles_AreNotServedOverHttp`: sentinel files under content root and `App_Data` return `404`.
- Live smoke: `/App_Data/geography.db` → `404`, `/geography.db` → `404`, `/` → `200 text/html`, `/api/countries` → `200 application/json`.

## 14. Acceptance criteria (Gherkin)
```gherkin
Scenario: Database files are not addressable
  Given a SQLite file under App_Data
  When I request /App_Data/geography.db
  Then the response is 404

Scenario: Files outside wwwroot are not served
  Given a .db file in the content root
  When I request /serving-guard.db
  Then the response is 404

Scenario: The app still works
  When I request /api/countries
  Then the response is 200 application/json
```

## 15. Definition of done
Preflight green; no `.db` in the project root; ADR-0013 recorded; serving regression test green.

## 16. Verification script
1. `dotnet test Weather.sln --filter Category=SingleHost`
2. Run the host; `curl -o /dev/null -w '%{http_code}' /App_Data/geography.db` → `404`.

## 17. Out of scope
Section 6.

## 18. Risks / notes
`App_Data` is safe *because it is not `wwwroot`* and is not registered with any file provider.

## 19. Sizing / BCP rationale
3 BCPs — host path resolution + factories + test + docs.

## 20. References
- `specs/adr/0013-sqlite-app-data-location.md`, `specs/adr/0006-per-module-data-isolation.md`
- https://learn.microsoft.com/aspnet/core/fundamentals/static-files
- https://learn.microsoft.com/aspnet/core/fundamentals/  (content root / web root)
