# Story e11s03 — Keep runtime SQLite data out of the build/publish artifact

| Field | Value |
|-------|-------|
| ID | e11s03 |
| Epic | e11 SQLite Data File Location |
| Status | todo |
| BCPs | 2 |
| Risk | P2 |
| Type | chore |
| ADR | ADR-0013 (amended) |

## 1. Summary
Make the exclusion of runtime SQLite files from the build/publish output explicit and verifiable, instead of
relying on the SDK's default `None` build action.

## 2. Context
`.db` files are `None` items (not `Content`) and are not copied to output or publish by default — confirmed by
`dotnet publish` (no `.db`/`App_Data` in the artifact) and by Microsoft Learn
([content items](https://learn.microsoft.com/visualstudio/msbuild/common-msbuild-project-items?view=visualstudio#content),
[CopyToPublishDirectory](https://learn.microsoft.com/dotnet/core/project-sdk/msbuild-props#item-metadata)).
That default is easy to regress (e.g. someone adds a `Content` glob) and there is no CI check, so the guarantee
is implicit.

## 3. User story
As a release engineer, I want a build-time guarantee that the published artifact never contains runtime SQLite
data, so a deployment cannot accidentally ship database files.

## 4. Business value
Prevents shipping stale/dev/seed data and keeps the release artifact a pure application.

## 5. In scope
- Explicit MSBuild metadata in `Weather.WebUI.csproj`: `Content Remove` + `None Update` with
  `CopyToOutputDirectory=Never` / `CopyToPublishDirectory=Never` for `App_Data\**\*`.
- A `scripts/check-publish-no-data.sh` guard that publishes the host and fails if any `*.db` / `App_Data`
  appears (wire into CI with `wire-ci`).
- Document the deployment contract: the artifact carries no data; the app creates `App_Data` at runtime; the
  directory must be writable/persistent (or `Database:DataDirectory` / `ConnectionStrings` point at such a
  location).

## 6. Out of scope
- Shipping a seed database (rejected: migrations + seeding run at runtime).
- CI provider wiring itself (covered by `wire-ci`).

## Requirements (delta)

### ADDED — published artifact excludes runtime data
The WebUI project explicitly excludes `App_Data\**\*` from output and publish, and a script asserts the
published artifact contains no SQLite data files.

## 7. Domain concepts
None.

## 8. Layer touchpoints
| Assembly | Change |
|----------|--------|
| `Weather.WebUI` | `Weather.WebUI.csproj` publish-exclusion metadata |
| `scripts/` | `check-publish-no-data.sh` |

## 9. API contract
Unchanged.

## 10. Dependencies
e11s01, e11s02.

## 11. Risks
| Risk | Mitigation |
|------|-----------|
| `<None Update>` glob matches nothing on a clean checkout | `Update` emits no error when empty; verified in preflight |
| A future `Content` glob re-includes the files | `Content Remove` + the publish guard script |
| Directory missing at runtime | Host creates `App_Data` before migrations (e11s01/e11s02) |

## 12. Implementation steps
1. Add the explicit exclusion metadata to `Weather.WebUI.csproj` → verify: `! find src/Weather.WebUI/bin -name '*.db' | grep -q .`
2. Add `scripts/check-publish-no-data.sh` → verify: `bash scripts/check-publish-no-data.sh`
3. Amend ADR-0013 + tech-stack with the publish/deploy contract → verify: `grep -q 'CopyToPublishDirectory' specs/adr/0013-sqlite-app-data-location.md`
4. Full preflight → verify: `dotnet build Weather.sln --warnaserror && dotnet format Weather.sln --verify-no-changes && dotnet test Weather.sln`

## 13. Test plan
- Publish guard script proves absence in the artifact.
- Existing 179 tests remain green.

## 14. Acceptance criteria (Gherkin)
```gherkin
Scenario: Publish contains no runtime data
  When the host is published in Release
  Then the artifact has no *.db and no App_Data directory

Scenario: The published app still works
  Given a clean publish directory
  When the app starts
  Then it creates App_Data and serves /api/countries with 200
```

## 15. Definition of done
Exclusion metadata present; publish guard green; ADR-0013 documents the deployment contract; preflight green.

## 16. Verification script
1. `bash scripts/check-publish-no-data.sh`
2. `find /tmp/<publish> -iname '*.db'` → empty.

## 17. Out of scope
Section 6.

## 18. Risks / notes
This is a build/packaging concern, not a runtime one — the runtime location remains ADR-0013.

## 19. Sizing / BCP rationale
2 BCPs — csproj metadata + guard script + docs.

## 20. References
- https://learn.microsoft.com/visualstudio/msbuild/common-msbuild-project-items?view=visualstudio#content
- https://learn.microsoft.com/dotnet/core/project-sdk/msbuild-props#item-metadata
- `specs/adr/0013-sqlite-app-data-location.md`
