# Story e16s02 — Runtime version surface (AppVersion, footer, /version, console startup line)

| Field | Value |
|-------|-------|
| ID | e16s02 |
| Epic | e16 Application & Artifact SemVer (host, platform) |
| Status | planned |
| BCPs | 5 |
| Risk | P0 |
| Type | feat |
| Delta | ADDED |

## 1. Summary
Read the stamped informational version once through a small `AppVersion` type and surface it in four cheap
places: the footer, a host-level `GET /version` endpoint, a one-time startup log, and (in e16s03) the OTel
resource. Replace the header's `Assembly.GetName().Version` so there is one source of truth. Depends on D4/D5.

## 2. Context
`AppHeader.razor` currently shows `typeof(AppHeader).Assembly.GetName().Version?.ToString(3)` — the assembly
*identity* version (`1.0.0`), not the build identity. `AppFooter.razor` shows the Kestrel host, OS, the
`.NET` framework description, and the copyright, but no version. `/health/live` and `/health/ready` exist but
carry no version. The unit-test
project has no bUnit; HTML is asserted through the in-process `WebApplicationFactory` (see
`WeatherExplorerPageTests`).

## 3. User story
As a support engineer, I want the app version visible in the footer and queryable over HTTP, so I can
identify the deployed build from a user's screen or a probe.

## 4. Business value
Turns "which build are you on?" into a one-second answer for users, support, and monitoring.

## 5. In scope
- `Services/AppVersion.cs`: parse `AssemblyInformationalVersionAttribute` of the entry assembly; expose
  `Version`, `InformationalVersion`, `Commit`; fall back to `0.0.0-unknown`; never throw.
- `Hosting/VersionEndpoints.cs` + mapping in `WebUiEndpointExtensions` next to `/health`.
- `AppFooter.razor`: show `v{Version}` (with the short commit) **immediately before the copyright**, and
  **remove the `.NET` `@RuntimeInformation.FrameworkDescription` span**; keep the existing design tokens.
- `AppHeader.razor`: consume `AppVersion` (or drop the duplicate version).
- Startup log line (Information): **one console line** printed before `app.Run()` —
  `GeoWeather {Version} ({Commit}) starting in {Environment}` — structured properties, no secrets. Lands on
  the console (JSON in Production via `AddJsonConsole`, human-readable in Development).

## 6. Out of scope
Telemetry resource (e16s03); a `/health` payload change (health stays about DB readiness); any module
endpoint; CI/build (e16s01).

## 7. Domain concepts
None (host concern). No module is referenced.

## 8. Layer touchpoints
| Assembly | Change |
|----------|--------|
| Weather.WebUI | `Services/AppVersion.cs` (new); `Hosting/VersionEndpoints.cs` (new); `Hosting/WebUiEndpointExtensions.cs`; `Components/Layout/AppFooter.razor`; `Components/Layout/AppHeader.razor`; `Program.cs` (startup log) |
| Weather.WebUI.UnitTests | `Services/AppVersionTests.cs` (new) |
| Weather.IntegrationTests | `Http/VersionEndpointTests.cs` (new); `Http/AppFooterTests.cs` (new) |

## 9. API contract (added)
`GET /version` → `200 application/json`:
```json
{ "version": "0.1.24-alpha",
  "informationalVersion": "0.1.24-alpha+g9a7eb6c819",
  "commit": "9a7eb6c819" }
```
Additive; existing endpoints unchanged. No `[ProducesResponseType]` error variants — the endpoint cannot
fail by design (it reads build metadata). Annotate with `200` and OpenAPI metadata anyway per ADR-0011.

## 10. Dependencies
e16s01 (the application must be stamped for this to show a real value).

## 11. Risks
| Risk | Mitigation |
|------|-----------|
| `InformationalVersion` absent when tests build the host | Fall back to `0.0.0-unknown`; unit test the fallback |
| Footer/header diverge | Both read `AppVersion`; header's `GetName().Version` removed |
| New CSS utility breaks the Tailwind drift guard | Reuse existing tokens; run `scripts/check-tailwind-css.sh` |
| Parsing NBGV's `+g<sha>` as if valid SemVer | `AppVersion.Commit` strips a leading `g`; `Version` is the part before `-`/`+` |

## 12. Requirements (delta)
- **ADDED** — a version reader and host endpoint.
- **MODIFIED** — the footer shows the version before the copyright and drops the `.NET` framework line; the
  header stops using `GetName().Version`.

## 13. Implementation steps
1. Add `AppVersion` with parsing + fallback and unit tests → verify: `dotnet test Weather.sln --filter Category=AppVersion`
2. Add `GET /version` and the OpenAPI annotation → verify: `dotnet test Weather.sln --filter Category=VersionEndpoint`
3. Show the version in the footer and switch the header to `AppVersion` → verify: `dotnet test Weather.sln --filter Category=AppFooter`
4. Add the **console startup line** before `app.Run()` (`GeoWeather {Version} ({Commit}) starting in {Environment}`) and a `StartupLogTests` assertion capturing the log record → verify: `dotnet test Weather.sln --filter Category=StartupLog`
5. Story gate: full preflight + `bash scripts/check-tailwind-css.sh` green → verify: `dotnet build Weather.sln --warnaserror && dotnet format Weather.sln --verify-no-changes && dotnet test Weather.sln`

## 14. Test plan
- Unit `AppVersionTests`: parses `0.1.24-alpha+g9a7eb6c819`, `0.1.24`, and garbage; commit extraction;
  fallback; `InformationalVersion` (not `GetName().Version`) is the source.
- Integration `VersionEndpointTests`: `GET /version` 200, JSON fields non-empty.
- Integration `AppFooterTests`: `GET /` HTML contains the version immediately before the copyright,
  does **not** contain the runtime framework description (`.NET`), and still contains the Kestrel host + OS.
- Integration `StartupLogTests`: booting the host emits exactly one Information record with `Version` and
  `Commit` properties (test logger provider); it does not contain an API key or connection string.
- Design/token: footer uses existing tokens; tailwind drift guard green.

## 15. Acceptance criteria (Gherkin)
```gherkin
Scenario: A user can report the version
  Given the app is running
  When I open the home page
  Then the footer shows the semantic version immediately before the copyright
  And the footer no longer shows the .NET framework description

Scenario: Ops can query the version
  When I GET /version
  Then the response is 200 with version, informationalVersion and commit

Scenario: The version is visible in the platform log
  Given the app is started
  Then one console line states the version and commit before the host starts listening

Scenario: The version is always readable
  Given build metadata is missing
  When AppVersion is read
  Then it returns 0.0.0-unknown and does not throw
```

## 16. Definition of done
Footer, `/version`, header, and startup log all show the same stamped version; tests cover parsing and the
fallback; Preflight green.

## 17. Verification script
1. `dotnet run --project src/Weather.WebUI` → open `/`; footer shows `v0.1.x+g…`.
2. `curl -s localhost:5121/version | jq` → fields match the footer.
3. Check the console log for the single startup version line.

## 18. Risks / notes
The footer already shows OS/framework; the version is added alongside, not replacing, that diagnostic info.

## 19. Sizing / BCP rationale
5 BCPs — reader + endpoint + footer + header + log, each with tests.

## 20. References
- `specs/adr/0019-application-and-artifact-versioning.md` (D4, D5)
- `src/Weather.WebUI/Components/Layout/AppFooter.razor`
- `src/Tests/Weather.IntegrationTests/Http/WeatherExplorerPageTests.cs` (HTML assertion pattern)
