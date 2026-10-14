# Security Plan

## Surface area
| Surface | Exposure | Mitigation |
|---------|----------|------------|
| `GET /api/*` | Public read | Input validation on route params; no secrets in responses |
| `POST /api/weather/notes` | Public write | FluentValidation (max length, non-empty); ProblemDetails on rejection |
| `POST /api/cities/favorites` | Public write | Uniqueness constraint (city, country); idempotent conflict → 409 |
| Upstream OpenWeatherMap call | Outbound | API key from config/user-secrets; timeout; bounded retry configured by `OpenWeather:Resilience` (ADR-0015). Retries must never log the request URI/query string (the key rides in `appid`); `AddResilienceHandler` does not enable Polly telemetry by default. |
| SQLite file | Local | Parameterized EF queries only (no raw SQL string concatenation) |

## Key handling
- `OpenWeather:ApiKey` is read from configuration. In development, `dotnet user-secrets`
  (`Weather.WebUI.csproj` carries a `UserSecretsId`); any environment can use `OpenWeather__ApiKey`;
  deployed environments use an App Service setting / Key Vault reference.
- The key is never logged and never returned by any endpoint.
- `appsettings.json` ships `OpenWeather:BaseUrl` only — no key, not even an empty one.
- `OpenWeatherOptionsValidator` requires the key outside Development (fail fast); Development starts and
  degrades weather calls to `502 DEPENDENCY_UNAVAILABLE` so UI work needs no key.

## Dependency risks
- Third-party runtime packages: MediatR, FluentValidation, EF Core providers, OpenAPI, ArchUnitNET (test-only).
- Slopcheck tags recorded in `tech-stack.md`; no `[SUS]`/`[SLOP]` runtime packages.

## OWASP quick pass
- **Injection:** EF Core parameterizes; no dynamic SQL.
- **Broken auth:** no auth in scope; documented as a deferred item.
- **Sensitive data exposure:** no PII stored; notes are user text with length limits.
- **XXE/Deserialization:** `System.Text.Json` with explicit DTOs; no polymorphic deserialization.
- **SSRF:** outbound URL is fixed by configuration, not user input.

## Verification
- `security-review` runs at epic start (build-epic step 0) for e03 and e04.
- Tasks tagged `security: medium|high` must verify "no new security findings in affected paths".
