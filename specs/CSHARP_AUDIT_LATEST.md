# C# Standards Audit — Weather.WebUI

> Status: findings fixed; enforcement added. Scope: `src/Weather.WebUI/**` (43 C# files).
> Reference: `docs/engineering/csharp-standards.md`.

## Method

1. `dotnet build Weather.sln --warnaserror` and `dotnet format --verify-no-changes` (the Preflight gates).
2. `dotnet build src/Weather.WebUI -p:AnalysisMode=All -p:TreatWarningsAsErrors=false` to surface every
   CA rule, then triaged each finding by hand.
3. `dotnet format style|analyzers --verify-no-changes --severity info` for IDE rules.
4. Manual sweep for null-forgiving, `async void`, `.Result`/`.Wait()`, `Task.Run`, `DateTime.Now`,
   `new HttpClient()`, broad catches, non-sealed types, and multi-type files.
   (`audit-csharp`'s `scripts/check-csharp.sh` is not installed in this repo.)

## Findings and resolution

| # | Finding | Severity | Resolution |
|---|---|---|---|
| 1 | Async controller actions lacked the `Async` suffix (`IDE1006` ×6) | MEDIUM | Renamed to `…Async`; routes unchanged, OpenAPI `operationId` changed; controller unit tests updated |
| 2 | One type per file violated in 3 files | MEDIUM | Split into `AddFavoriteRequest.cs`, `AddNoteRequest.cs`, `LocationModel.cs`, `TemperatureModel.cs`, `WindModel.cs` |
| 3 | `CspReportEndpoint` used `ILoggerFactory` + a string category | MEDIUM | Now `ILogger<CspReportEndpoint>` (§10); the type is non-static so it can be the logger category |
| 4 | Enforcement gaps — the standard's rules did not gate the build | HIGH *(for the goal)* | Added a scoped `.editorconfig` section (below) |
| 5 | `StartsWithSegments` without `StringComparison` (`CA1307`) | LOW | `StringComparison.Ordinal` |
| 6 | Mutable collection setters (`CA2227`) | LOW | Suppressed on the options types with justification (binding requires a setter); CA2227 stays enabled for everything else |
| 7 | Constant array allocated per call (`CA1861`) | LOW | Hoisted to a `static readonly` field |
| 8 | `Created(string, …)` instead of `Created(Uri, …)` (`CA2234`) | LOW | `Created(new Uri(…, UriKind.Relative), …)` |
| 9 | `var` / collection-expression nits (`IDE0007`, `IDE0305`) | LOW | Applied |
| 10 | `WebUi` vs `WebUI` type-name inconsistency | LOW | Noted; left for a dedicated rename (no rule backs it) |

## Not violations — deliberately rejected (verified, not assumed)

| Rule | Why not applied |
|---|---|
| `CA2007` (ConfigureAwait) | Standards §6: do **not** add `ConfigureAwait(false)` in ASP.NET Core |
| `CA1515` (make types internal) | Types are consumed by the test projects; MVC needs public controllers |
| `ASP0027` (public `partial Program`) | Kept deliberately for `WebApplicationFactory<Program>` |
| `CA1032` (standard exception constructors) | `WeatherApiException` intentionally requires an `ApiProblem` |
| `CA1062` (null-check controller `request`) | `[ApiController]` + `[FromBody]` returns `400 MALFORMED_REQUEST` before the action |
| `CA1812` (handler "never instantiated") | `GlobalExceptionHandler` is DI-instantiated |
| `CA1056`/`CA1848` in the Weather module | Out of scope (module, not the host) |

## Enforcement added

A `[src/Weather.WebUI/**/*.cs]` section in `.editorconfig` (scoped to the host so the modules can be
audited separately):

- **Naming/style gates:** `async_methods_end_with_async` → `warning`, `namespace_match_folder` →
  `warning`, `readonly_field` → `warning`, `prefer_collection_expression` → `warning`.
- **Analyzer gates:** `CA1307`, `CA1861`, `CA2227`, `CA2234` → `warning`.
- **Deliberately disabled:** `CA2007`, `CA1515`, `ASP0027` (see above).

With `TreatWarningsAsErrors=true`, the CA rules fail `dotnet build`; the naming/style rules fail
`dotnet format --verify-no-changes`. Both are Preflight gates. **Proven** by temporarily adding a
violating file: `dotnet build --warnaserror` failed on `CA1307` and `dotnet format style` failed on
`IDE1006`.

## Evidence

| Command | Result |
|---|---|
| `dotnet build Weather.sln --warnaserror` | 0 warnings, 0 errors |
| `dotnet format Weather.sln --verify-no-changes` | clean |
| `dotnet test Weather.sln` | 248 passed, 0 failed |
| Deliberate violation probe | build failed (CA1307) / format failed (IDE1006) |

## Decisions (interview)

1. Enforcement strategy — tighten `.editorconfig`/analyzers + fix code (not code-only).
2. Async-suffix rename — apply to the 6 actions and update tests.
3. One type per file — split all three files.
4. Suppressions — hybrid (`.editorconfig` for never-applicable rules, `[SuppressMessage]` for intentional designs).
5. Enforcement scope — `src/Weather.WebUI/**` only.
6. Commits — two (code fixes; enforcement config).

## Out of scope

- The modules (`Weather.Modules.*`, `Weather.BuildingBlocks.*`) — they have their own findings
  (`CA2234`, `CA1848`/`CA1873`, `CA1032`, `CA2007`, …) and should be a separate audit.
- The test projects.
- Behavior/HTTP-contract changes (this was a code-quality change only).

---

# Follow-up — Modules & BuildingBlocks

> Scope: the **production assemblies** of every module (`src/Modules/*/Weather.Modules.*/`) and
> `src/BuildingBlocks/Weather.*/`. Each module's `Tests` project is out of scope.

## Enforcement extended (same gates)

`.editorconfig` now has matching sections for those paths with the same curated rules as the WebUI host:
`namespace_match_folder`, `readonly_field`, `prefer_collection_expression` and `CA1307`, `CA1861`,
`CA2227`, `CA2234`; `CA2007`/`CA1515` stay disabled.

**Deliberate difference:** the `Async`-suffix naming rule is **not** applied here. Seven async methods are
MediatR `Handle` implementations (`GetFavoritesQueryHandler`, `AddFavoriteCityCommandHandler`,
`AddWeatherNoteCommandHandler`, `GetCurrentWeatherQueryHandler`, `GetCitiesByCountryQueryHandler`,
`GetCountriesQueryHandler`, `ValidationBehavior`) — interface methods that cannot be renamed. The rule
stays WebUI-only.

## Fixes under those gates

| Finding | File | Fix |
|---|---|---|
| `CA2234` | `OpenWeatherMap/OpenWeatherMapService.cs` | pass a `Uri` (not a `string`) to `HttpClient.GetAsync` |
| `IDE0305` ×2 | `ValidationBehavior.cs` | collection expressions |
| `IDE0305` | `Favorites/GetFavoritesQueryHandler.cs`, `FavoriteCityReadRepository.cs`, `Geography/GetCitiesByCountryQueryHandler.cs`, `GetCountriesQueryHandler.cs` | collection expressions |

Production `CA1307`/`CA1861`/`CA2227`, and `namespace_match_folder`/`readonly_field`, had no violations.

## Glob correction (found while proving enforcement)

The scoped sections originally used `[path/**/*.cs]`, which — per editorconfig `**` semantics — requires
**at least one** directory, so files sitting directly in a project root (e.g. `Program.cs`) were not
matched. Changed all scoped sections to `[path/**.cs]`, which matches zero or more segments. **Proven**
with root-level and subfolder probes (`IDE0130` for modules, `IDE1006` for WebUI).

## Backlog reduction (this change)

Two backlog items were fixed and the corresponding rules added to the scoped gates:

| Finding | Fix | Gate |
|---|---|---|
| `CA1849` | The three database initializers `await …MigrateAsync(cancellationToken)` instead of blocking on `Database.Migrate()` (standard §6) | `dotnet_diagnostic.CA1849 = warning` |
| `CA1056` | `OpenWeatherOptions.BaseUrl` is a `Uri`; the typed client assigns it directly. Configuration binding of a string to `Uri` is covered by the existing wiring tests. | `dotnet_diagnostic.CA1056 = warning` |

## Remaining backlog — reported, deliberately not fixed (`AnalysisMode=All`-only)

| Rule | Why it is not fixed |
|---|---|
| `CA1032` (~50) | Domain/application exceptions carry domain data (e.g. `CountryCode`). The parameterless and `(string)` constructors CA1032 wants would allow invalid instances; a parameterized constructor is the correct design (§5). |
| `CA1848`/`CA1873` | Logging volume is tiny; source-generated `LoggerMessage`/`[LoggerMessage]` is a performance optimization, not a standard requirement (§10 asks for `ILogger<T>` + structured properties, which is already met). |
| `CA1040`/`CA1711` | `IIntegrationEvent` is an intentional marker interface; `IIntegrationEventHandler` is the clear, conventional name. Both are deliberate design choices. |
| `CA2000` | Occurs in **test** projects (disposal in test helpers); the production assemblies are clean. |
| `CA1707` | Test-method names intentionally use underscores for readability (Arrange/Act/Assert-style names); test projects are out of scope. |

None of these are enabled at `AnalysisLevel=latest`; they are recorded here as a known, accepted backlog.

## Evidence

| Command | Result |
|---|---|
| `dotnet build Weather.sln --warnaserror` | 0 warnings, 0 errors |
| `dotnet format Weather.sln --verify-no-changes` | clean |
| `dotnet test Weather.sln` | 248 passed, 0 failed |
| Root/subfolder probes | `IDE0130` (modules) and `IDE1006` (WebUI) fail the gates; `CA2234` fails the build |
| Test-project check | module `Tests` paths are excluded by the section globs |
