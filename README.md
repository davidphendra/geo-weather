<div align="center">
  <img src="src/Weather.WebUI/wwwroot/img/geoweather-mark.svg" alt="GeoWeather" width="96" height="96" />
  <h1>GeoWeather</h1>
  <p><em>Pick a country and city, see trustworthy current weather, and save notes and favorite cities.</em></p>
</div>

---

## Table of Contents

- [Overview](#overview)
- [Key Features](#key-features)
- [Technology Stack](#technology-stack)
- [Quick Start](#quick-start)
- [Architecture](#architecture)
- [Repository Structure](#repository-structure)
- [Configuration](#configuration)
- [Development Guide](#development-guide)
- [Security](#security)
- [Observability](#observability)
- [Testing](#testing)
- [Deployment](#deployment)
- [Troubleshooting](#troubleshooting)
- [Known Limitations](#known-limitations)
- [Contributing](#contributing)

---

## Overview

🌤️ **GeoWeather** is a small, production-grade weather application built as a **modular monolith**
(Onion architecture + CQRS/MediatR). A user chooses a country and city, views current conditions, and
saves weather notes or favorite cities.

- 🎯 **Who it is for** — someone who wants a quick, readable weather snapshot; and a developer who wants to
  assess clean boundaries, CQRS, EF Core usage, and test quality.
- 🧱 **Shape** — one deployable host serving both a Blazor UI and a REST API, divided into three bounded
  contexts (modules), each its own assembly set with its own data.
- ✅ **Promise** — all automated tests run **offline**, with zero live network calls.
- 📄 **Docs-first** — requirements, decisions, and evidence live in [`specs/`](#repository-structure); the
  agent workflow is defined in [`AGENTS.md`](AGENTS.md) and [`CONVENTIONS.md`](CONVENTIONS.md).
- 🚀 **Live app** — <https://geo-weather-a6ffe8hwfpbmfgcv.indonesiacentral-01.azurewebsites.net/>
  (Azure App Service, deployed from `main` by [`azure-pipelines.yml`](azure-pipelines.yml)).

## Key Features

- 🔎 **Location explorer** — country → city cascade with searchable selection.
- 🌡️ **Current conditions** — temperature (stored in Celsius, shown in °C/°F), humidity, wind, pressure,
  visibility, sky condition, and a derived dew point.
- 📅 **Five-day forecast** — daily cards aggregated from the OpenWeather 5 day / 3 hour forecast (ADR-0017).
- 🗺️ **City map** — the selected city drawn with Leaflet + OpenStreetMap tiles, with a graceful
  "unavailable" state (ADR-0018).
- ⭐ **Favorites** — save cities; a duplicate returns `409 DUPLICATE_RESOURCE`.
- 📝 **Weather notes** — attach a note (and the temperature at the time) to a city.
- ✅ **RFC 9457 errors** — every error is Problem Details with a stable `code` and a `traceId` (ADR-0011).
- 📈 **Observability** — OpenTelemetry metrics, traces, and logs exported to Azure Monitor (ADR-0014).

### HTTP API

| Method | Route | Purpose |
|--------|-------|---------|
| `GET` | `/api/countries?search=` | Country catalog (optionally filtered) |
| `GET` | `/api/countries/{countryCode}/cities` | Cities of one country |
| `GET` | `/api/weather/{cityName}?countryCode=` | Current weather for a city |
| `GET` | `/api/weather/{cityName}/forecast?countryCode=` | Five-day forecast for a city |
| `GET` | `/api/cities/favorites` | List saved favorite cities |
| `POST` | `/api/cities/favorites` | Save a favorite city → `201` + `Location` |
| `POST` | `/api/weather/notes` | Save a weather note → `201` + `Location` |
| `GET` | `/health/live`, `/health/ready` | Liveness / readiness probes |
| `GET` | `/version` | Running build version and source commit |

## Technology Stack

| Concern | Choice | Notes |
|---------|--------|-------|
| Language / runtime | C# · .NET 10 (SDK `10.0.401`) | `nullable`, `ImplicitUsings`, warnings as errors |
| API | ASP.NET Core MVC controllers | Thin, one area per module (ADR-0009) |
| UI | Blazor Web App (`InteractiveServer`) | Hosted in the same process as the API (ADR-0012) |
| CQRS | MediatR 12.x | Pinned (ADR-0004) |
| Validation | FluentValidation 12 | Pipeline behavior per module → `422` |
| ORM / data | EF Core 10 + SQLite | One `DbContext` + one DB file per persisting module (ADR-0006) |
| Resilience | `Microsoft.Extensions.Http.Resilience` | Configurable retry + timeouts (ADR-0015) |
| Observability | Microsoft OpenTelemetry distro → Azure Monitor | Metrics/traces/logs (ADR-0014) |
| UI components | Fluent UI Blazor | Country/city pickers (ADR-0020) |
| Tests | xUnit · Shouldly · NSubstitute · NetArchTest · Playwright | All .NET tests offline |

Package versions are centrally managed in [`Directory.Packages.props`](Directory.Packages.props); full
rationale is in [`specs/tech-architecture/tech-stack.md`](specs/tech-architecture/tech-stack.md).

## Quick Start

**Prerequisites**

- 🟣 [.NET SDK 10.0.401](https://dotnet.microsoft.com/) — pinned in [`global.json`](global.json) (`rollForward: latestFeature`)
- 🟢 Node.js 20.x — only for the Tailwind CSS build and the Playwright visual tests
- 🔧 `git` with **full history** — versioning (Nerdbank.GitVersioning) reads git; a shallow clone fails the build

**Run it**

```bash
git clone <repository-url>
cd Weather

dotnet restore Weather.sln

# Provide the OpenWeatherMap API key (never committed). Development is the only environment
# that tolerates a missing key — elsewhere the host fails fast at startup.
dotnet user-secrets set "OpenWeather:ApiKey" "<your-key>" --project src/Weather.WebUI

dotnet run --project src/Weather.WebUI
```

- 🌐 UI / API: <http://localhost:5121> (HTTPS profile: <https://localhost:7137>)
- 🩺 Health: `curl /health/live` · `curl /health/ready`
- 🏷️ Version: `curl /version`
- ⚠️ **No key?** Development still starts; weather requests return `502 DEPENDENCY_UNAVAILABLE` so UI work
  needs no live provider.
- 🔁 Alternative to user-secrets: the environment variable `OpenWeather__ApiKey=<key>` (any environment).

## Architecture

GeoWeather is a **modular monolith** sliced by bounded context, following
`kgrzybek/modular-monolith-with-ddd`. Each module is internally **Onion-layered** and uses **CQRS**.
There is one deployable unit.

| Module | Kind | Own data | DbContext | Publishes | Consumes |
|--------|------|----------|-----------|-----------|----------|
| **Geography** | Supporting | `geography.db` | `GeographyContext` | `CityAddedIntegrationEvent` | — |
| **Weather** | **Core** | none (external) | — | — | — |
| **Journal** | Supporting | `journal.db` | `JournalContext` | — | `CityAddedIntegrationEvent` |

> **Journal** is the merged Favorites + Notes bounded context (ADR-0016): it owns `FavoriteCities`,
> `WeatherNotes`, and one local `City` projection, and exposes `IFavoritesService` + `INotesModule`.

**Boundary rules — violations are build-breaking defects**

- 🔒 A module references another module **only** through that module's `IntegrationEvents` assembly.
- 🔁 Modules communicate **only asynchronously** via the Events Bus — no direct cross-module calls.
- 🗄️ Each module owns its data: its own `DbContext` and its own SQLite file; no cross-module joins.
- 🏛️ Inside a module, dependencies point inward: `Domain ← Application ← Infrastructure`.
- 🧭 Enforced by executable architecture tests: `ModuleTests` (cross-module), `LayersTests` (per module),
  and `ApiTests` (host isolation) — NetArchTest.

Cross-module flow (the only one):

```text
Geography ──CityAddedIntegrationEvent──► In-memory Events Bus ──► Journal.CityAddedIntegrationEventHandler
```

Design decisions are recorded as ADRs in [`specs/adr/`](specs/adr/) — start with **0005** (modular monolith),
**0006** (per-module data isolation), **0012** (single host), and **0016** (Favorites + Notes → Journal).

## Repository Structure

```text
src/
  BuildingBlocks/
    Weather.BuildingBlocks.Domain/          DDD primitives (Entity, AggregateRoot, ValueObject, ...)
    Weather.BuildingBlocks.Application/     ICommand, IQuery, validation + processing pipeline, Events Bus
    Weather.BuildingBlocks.Infrastructure/ EF base config, Outbox, InMemoryEventsBus, UnitOfWork, Clock
    Tests/Weather.BuildingBlocks.Tests/     BuildingBlocks unit tests
  Modules/
    Geography/   Weather.Modules.Geography.{Domain,Application,Infrastructure,IntegrationEvents}
    Weather/     Weather.Modules.Weather.{Domain,Application,Infrastructure,IntegrationEvents}
    Journal/     Weather.Modules.Journal.{Domain,Application,Infrastructure,IntegrationEvents}
                 (each with a Tests/Weather.Modules.<Module>.Tests project)
  Weather.WebUI/      The single host: Components/ (Blazor UI) + Api/Modules/<Module>/*Controller.cs
  Tests/
    Weather.ArchTests/           Cross-module + API boundary tests (NetArchTest)
    Weather.IntegrationTests/    End-to-end host tests (offline)
    Weather.WebUI.UnitTests/     Controller unit tests
specs/    Planning, scope, ADRs, epics, bugs, verifications (docs-first)
docs/     Engineering standards + the observability runbook
ops/      Azure Monitor workbook + alert rules (as code)
scripts/  Guards: check-layout, check-publish-no-data, check-tailwind-css, check-version
```

The solution [`Weather.sln`](Weather.sln) contains 23 projects: 3 BuildingBlocks, 3 modules
(4 assemblies each), the host, and 8 test projects.

## Configuration

Settings ship in [`appsettings.json`](src/Weather.WebUI/appsettings.json) (Development overrides in
`appsettings.Development.json`). Secrets are supplied per environment — never committed.

| Key | Default | Purpose |
|-----|---------|---------|
| `OpenWeather:ApiKey` | *(secret)* | Provider key; **required outside Development** (fail fast) |
| `OpenWeather:BaseUrl` | `https://api.openweathermap.org/` | Provider base URL |
| `OpenWeather:Resilience:MaxRetryAttempts` | `3` | Retries after the first attempt |
| `OpenWeather:Resilience:RetryBaseDelaySeconds` | `1` | Base delay (exponential backoff + jitter) |
| `OpenWeather:Resilience:AttemptTimeoutSeconds` | `10` | Per-attempt timeout |
| `OpenWeather:Resilience:TotalTimeoutSeconds` | `30` | Total call cap |
| `Database:DataDirectory` | `App_Data` | SQLite location, outside `wwwroot` (ADR-0013) |
| `Database:Files:Geography` / `Journal` | `geography.db` / `journal.db` | Per-module DB file names |
| `Location:DefaultCountryCode` / `DefaultCityName` | `ID` / `Jakarta` | Initial UI selection |
| `Observability:Enabled` | `false` | Master switch for telemetry |
| `Observability:ExportTarget` | `AzureMonitor` | `AzureMonitor` / `Console` / `Otlp` |
| `Observability:SamplingRatio` | `0.1` (prod) | Trace sampling |
| `Csp:EnforceInDevelopment` | `false` | CSP enforce vs. report-only |
| `ConnectionStrings:*` | *(derived)* | Override the per-module SQLite paths if needed |

Supply a secret per environment: `dotnet user-secrets` (Development), `OpenWeather__ApiKey` (environment
variable), or an App Service setting / Key Vault reference (deployed).

## Development Guide

📚 Read [`CONVENTIONS.md`](CONVENTIONS.md) before any git or GitHub operation, and
[`AGENTS.md`](AGENTS.md) for the agent workflow. C# and REST rules are mandatory:
[`docs/engineering/csharp-standards.md`](docs/engineering/csharp-standards.md) and
[`docs/engineering/rest-api-standards.md`](docs/engineering/rest-api-standards.md).

- 🧹 **Preflight (must be green before any forward work):**
  ```bash
  dotnet build Weather.sln --warnaserror && dotnet format Weather.sln --verify-no-changes && dotnet test Weather.sln
  ```
- 🧱 **Layout guard:** `bash scripts/check-layout.sh` — fails on any forbidden cross-module reference.
- 🎨 **Tailwind guard:** `bash scripts/check-tailwind-css.sh` — fails if the committed `app.css` drifted.
- 🧭 **Specs-first:** no code without a story in [`specs/epics/`](specs/epics/). Update `specs/` before writing code.

## Security

- 🔑 **Key handling** — `OpenWeather:ApiKey` comes from configuration only (user-secrets / env / Key Vault).
  It is never logged and never returned by any endpoint. Outside Development the host fails fast when it is
  missing.
- 🛡️ **CSP Level 3** — a per-request nonce policy is applied by the host (`src/Weather.WebUI/Security/`);
  violations are reported to `/_csp/report`.
- 🧱 **Injection** — EF Core parameterizes queries only; there is no raw or dynamic SQL.
- 🚫 **Auth** — no authentication/authorization in scope (a deferred item); no PII is stored.
- 🙈 **Secrets** — `.gitignore` excludes `appsettings.*.local.json`, `.env`, `*.db`, and `App_Data/`.

Full detail: [`specs/tech-architecture/SECURITY_PLAN_LATEST.md`](specs/tech-architecture/SECURITY_PLAN_LATEST.md).

## Observability

Signals are OpenTelemetry **metrics, traces, and logs** exported to **Azure Monitor** (Application Insights)
via the Microsoft OpenTelemetry distro (ADR-0014).

- ▶️ Telemetry is **off by default**; enable with `Observability__Enabled=true`. The `dotnet run` launch
  profile enables it with the `Console` exporter locally.
- 🩺 Endpoints: `/health/live`, `/health/ready`, `/version`.
- 🔍 Every RFC 9457 error carries a `traceId` (the OpenTelemetry trace id) — **search that id in Application
  Insights** to trace a reported error.
- 📊 Metrics are bounded-cardinality: never a city name, note text, id, URL, or exception message.

Runbook: [`docs/operations/observability.md`](docs/operations/observability.md) ·
Metric catalog: [`specs/tech-architecture/OBSERVABILITY_PLAN_LATEST.md`](specs/tech-architecture/OBSERVABILITY_PLAN_LATEST.md) ·
Dashboards/alerts as code: [`ops/azure-monitor/`](ops/azure-monitor/).

## Testing

> All automated .NET tests run **offline**, with zero live network calls.

- 🧪 **Unit** — domain math (temperature conversion, dew point), handlers (NSubstitute + EF Core InMemory).
- 🏛️ **Architecture** — NetArchTest for layer direction and module boundaries (per module + cross-module).
- 🌐 **Integration** — `WebApplicationFactory` with a stub `IWeatherProvider` and SQLite `:memory:`.
- 🎭 **Visual parity** — Playwright (`npm run parity:styles`) checks layout across breakpoints; run locally,
  not in CI.
- 🛡️ **Guards** — `scripts/check-{layout,publish-no-data,tailwind-css,version}.sh`.

```bash
dotnet test Weather.sln                 # unit + architecture + integration
dotnet test Weather.sln --filter Category=Observability
npm --prefix src/Weather.WebUI run parity:styles   # visual parity (starts the host with a stub key)
```

Eight test projects; plan and coverage targets: [`specs/tech-architecture/TEST_PLAN_LATEST.md`](specs/tech-architecture/TEST_PLAN_LATEST.md).

## Deployment

The pipeline [`azure-pipelines.yml`](azure-pipelines.yml) (Azure DevOps) runs:

1. 🧪 **CI** (on PR and `main`) — full clone → restore → `build -warnaserror` → `format --verify-no-changes`
   → Tailwind guard → all offline tests + coverage → publish → version guard.
2. 🚀 **CD** (on `main`, after CI) — **automatic**, no approval gate; zip-deploys the publish
   artifact to a Linux Azure App Service.

Versioning is git-derived (Nerdbank.GitVersioning, single source `version.json`); the run number is set to the
semantic version, and `scripts/check-version.sh` rejects a bogus `0.0.0`. Plan and one-time setup:
[`specs/tech-architecture/AZURE_PIPELINE_PLAN_LATEST.md`](specs/tech-architecture/AZURE_PIPELINE_PLAN_LATEST.md)
and [`VERSIONING_PLAN_LATEST.md`](specs/tech-architecture/VERSIONING_PLAN_LATEST.md).

Production runs at <https://geo-weather-a6ffe8hwfpbmfgcv.indonesiacentral-01.azurewebsites.net/>
— health probes: `/health/live` · `/health/ready` · `/version`.

## Troubleshooting

- 🌐 **Weather returns `502 DEPENDENCY_UNAVAILABLE`** — the OpenWeather key is missing or invalid. Set
  `OpenWeather:ApiKey` (Development tolerates a missing key; other environments fail fast at startup).
- 🏷️ **Build error `WX0001`, or version `0.0.0`** — no semantic version could be derived. The `.git` folder
  must be present and the clone must **not** be shallow (`git fetch --unshallow`).
- 🗄️ **SQLite error 14 on startup** — the parent directory of a configured `ConnectionStrings` path is
  missing. Point `Database:DataDirectory` at a writable folder (the host creates it at runtime).
- 🎨 **`check-tailwind-css.sh` fails** — the committed `app.css` is stale; run
  `npm --prefix src/Weather.WebUI run build:css` and commit the result.
- 📦 **NuGet restore fails** — package versions are centralized in `Directory.Packages.props`; use
  `dotnet restore Weather.sln` from the repo root.

## Known Limitations

- 🚫 **No authentication / user accounts** — deferred; there is no login requirement (ADR-0005).
- 📉 **No hourly or historical forecast, and no forecast caching** — five daily cards satisfy the
  requirement; caching is out of scope (ADR-0017).
- 🔌 **No circuit breaker or rate limiter** on the weather provider — only bounded retry + timeouts
  (ADR-0015).
- 🧾 **Log `service.name`** — the OpenTelemetry distro's logging resource is separate, so logs use the
  SDK-default `service.name` (tracked in e16).
- 🗃️ **SQLite** — chosen for local simplicity over SQL Server (ADR-0002).

## Contributing

1. ✅ **Keep Preflight green before any forward work:**
   ```bash
   dotnet build Weather.sln --warnaserror && dotnet format Weather.sln --verify-no-changes && dotnet test Weather.sln
   ```
2. 🌿 **Branch and merge** — branch off `main` (e.g. `feature/<epic>-<slug>`). If you collaborate, open a PR
   and wait for CI (`gh pr checks`) plus review; otherwise merge to `main` **only** when Preflight is green.
3. 💬 **Commits** — Conventional Commits, no AI attribution. Fix-or-log every defect
   ([`specs/bugs/registry.yaml`](specs/bugs/registry.yaml)).
4. 🧭 **Specs-first** — plan work in `specs/epics/` before writing code; use the agent workflow in `AGENTS.md`.

---

<div align="center"><sub>Built with .NET 10 · Blazor · EF Core · MediatR — see <a href="specs/">specs/</a> for the full decision record.</sub></div>
