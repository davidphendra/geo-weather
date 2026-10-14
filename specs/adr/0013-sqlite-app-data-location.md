# ADR-0013 — SQLite data files live in `App_Data`, outside the web root

**Status:** Proposed (needs user confirmation)
**Date:** 2025-10-15
**Relates to:** ADR-0006 (per-module data isolation), ADR-0012 (single host)

## Context
Each persisting module (Geography, Favorites, Notes) defaults to a relative connection string
(`Data Source=geography.db`, …), so SQLite created the files in the process working directory —
`src/Weather.WebUI/` when running the single host. A reviewer raised a security concern: could a deployed
client download those database files directly?

## Finding — what Microsoft documents
| Source | Statement |
|--------|-----------|
| ASP.NET Core fundamentals — [Content root / Web root](https://learn.microsoft.com/aspnet/core/fundamentals/?view=aspnetcore-10.0#content-root) | Content root is the base path for config files "and data files (`.db`)". Web root defaults to `{CONTENT ROOT}/wwwroot`. |
| [Serve static files in ASP.NET Core apps](https://learn.microsoft.com/aspnet/core/fundamentals/static-files?view=aspnetcore-10.0) | "By default, static files are only served from the web root directory and its sub-directories. … **Only files in the `wwwroot` folder are addressable**, so you don't need to worry about the rest of your code." |
| Same page — [Security considerations](https://learn.microsoft.com/aspnet/core/fundamentals/static-files?view=aspnetcore-10.0#security-considerations-for-static-files) | "`UseStaticFiles` … can leak secrets. … Carefully review which directories are enabled. … The entire directory and its sub-directories become publicly accessible. **Store files suitable for serving to the public in a dedicated directory, such as `<content_root>/wwwroot`. Separate these files from MVC views, Razor Pages, configuration files, etc.**" |
| Same page — [Change the web root](https://learn.microsoft.com/aspnet/core/fundamentals/static-files?view=aspnetcore-10.0#change-the-web-root-directory) | Files can be excluded from publish with `<Content Update="wwwroot\local\**\*.*" CopyToPublishDirectory="Never" />`. |

Conclusions:
1. **The reported risk (direct download) only exists if the `.db` files are inside `wwwroot`.** The files were
   in the content root, not `wwwroot`, so they were already not addressable. The concern was valid in principle
   but not an active exposure in the previous layout.
2. Microsoft does **not** canonically prescribe an `App_Data` folder for ASP.NET Core — that is a legacy
   ASP.NET/Web Forms convention (classic deployment docs reference `App_Data` for SQL Server Compact files).
   What Microsoft *does* prescribe is the principle: keep data/config **outside** `wwwroot`, and keep everything
   publicly served under `wwwroot`. A dedicated `App_Data` directory satisfies that principle and matches the
   repository's existing `.gitignore` entry.
3. Never point `UseStaticFiles`/`UseDirectoryBrowser`/`MapStaticAssets` at the data directory.

## Decision
Store SQLite files in **`<content root>/App_Data/`** and make that non-negotiable:

1. A `Database` configuration section declares the folder and file names:
   `"Database": { "DataDirectory": "App_Data", "Files": { "Geography": "geography.db", … } }`.
   The host binds it (`DatabaseOptions`), resolves `DataDirectory` against `builder.Environment.ContentRootPath`
   when it is relative (absolute paths are honoured), and derives the `ConnectionStrings:{name}` defaults.
   Defaults apply **only** when no connection string is configured, so `appsettings`, environment variables,
   user-secrets, and tests still override them.
2. The host creates the parent directory of every resolved SQLite connection string — the configured default
   **and** any explicit `ConnectionStrings` override — before the module `IHostedService` initializers run
   `Database.Migrate()`. SQLite creates a database file but not its parent directory, so a path on a mounted
   volume would otherwise fail at startup with `unable to open database file`.
3. `App_Data` is never registered with static-file middleware or directory browsing. It stays outside `wwwroot`.
4. EF design-time factories resolve to the same `App_Data` folder so `dotnet ef` tooling is consistent.
5. A regression test writes sentinel `.db` files under the content root and `App_Data` and asserts both return
   `404` (see `DatabaseFiles_AreNotServedOverHttp`).
6. **Publish/deployment contract:** the release artifact carries **no** SQLite data. The WebUI project explicitly
   excludes `App_Data\**\*` (`Content Remove` + `CopyToOutputDirectory`/`CopyToPublishDirectory = Never`), and
   `scripts/check-publish-no-data.sh` publishes the host and fails if any `*.db`/`App_Data` appears. The app
   creates `App_Data` at runtime, so deployments only need to provide a writable, persistent location (or
   override `Database:DataDirectory` / `ConnectionStrings`). Shipping the databases is rejected: it would freeze
   dev/seed state and risk exposing data.

## Consequences
- Positive: data files are structurally separated from the served web root and from application code/config.
- Positive: explicit absolute path removes the working-directory ambiguity of `Data Source=x.db`.
- Positive: the location is overridable for real deployments (a mounted volume, `%HOME%\site` on Azure App
  Service, or a server database later).
- Negative: the directory must be created (handled) and must be treated as **persistent, writable** storage in
  deployment; containers with ephemeral filesystems lose data unless a volume is mounted.
- Negative: SQLite remains single-node; multi-instance hosting requires a change of provider (out of scope).
- Note: `App_Data` under the content root is *not* protected by the framework the way `wwwroot` is "special" —
  it is protected precisely because it is **not** `wwwroot` and is **not** wired into any file provider.

## Alternatives
- **Keep the files in the content root.** Not addressable, but mixes data with app binaries/config and leaves
  the working-directory-relative default. Rejected for clarity.
- **`wwwroot/data`.** Would be directly downloadable — rejected outright.
- **OS temp/app-data folder (e.g. `Environment.SpecialFolder.LocalApplicationData`).** Good for desktop, awkward
  for Linux containers and Azure App Service persistence. Left as a configurable override.
