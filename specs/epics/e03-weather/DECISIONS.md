# e03 Weather Module — Locked Decisions

Resolved during planning (Weather module only). These refine ADR-0003 and ADR-0007.

| # | Topic | Decision | Effect |
|---|-------|----------|--------|
| Q1 | Dew point rounding | **A — clamp** `DewPoint = min(derived, temperature)` | Guarantees the invariant `dewPoint ≤ temperature`; deterministic P0 test |
| Q2 | Numeric type | **A — `decimal`** throughout the domain; `(decimal)` at the mapper boundary | Stable 1-decimal rounding; record equality without tolerances |
| Q3 | Location | **A — omit coordinates**; `Location(city, countryCode)` only | Matches the required payload; YAGNI — add `GeoCoordinate` later if needed |
| Q4 | Missing wind direction | **B — nullable** `DirectionDegrees`; `Cardinal` returns `"Variable"` when null | Honest for calm/variable winds; `WindDto.DirectionDegrees` is `int?` |
| Q5 | API key hygiene | **C — `DelegatingHandler`** appends `appid` **and** the named client's log category is `Warning` | Key absent from code, exceptions, and logs |
| Q6 | `SkyCondition` | **A — typesafe value object** with static instances + `FromProvider` | Normalization stays in the domain; DTO maps `.Value`; no JSON enum config |

## Resulting domain shapes

```csharp
// Q1
public static DewPoint Derive(Temperature temperature, RelativeHumidity humidity)
{
    ...
    var derived = (decimal)(b * alpha / (a - alpha));
    return new DewPoint(Math.Min(derived, temperature.Celsius));
}

// Q2 — decimal everywhere
public decimal Celsius { get; }        // Temperature / DewPoint
public decimal SpeedMetersPerSecond { get; }   // WindInfo
public decimal Hectopascals { get; }   // Pressure
public decimal Meters { get; }         // Visibility

// Q4 — nullable direction
public sealed record WindInfo
{
    public decimal SpeedMetersPerSecond { get; }
    public int? DirectionDegrees { get; }
    public string Cardinal => DirectionDegrees is null
        ? "Variable"
        : CardinalDirection.FromDegrees(DirectionDegrees.Value);
    ...
}
public sealed record WindDto(decimal SpeedMetersPerSecond, int? DirectionDegrees, string Cardinal);

// Q5 — key handler + log level
"Logging:LogLevel:System.Net.Http.HttpClient.Weather": "Warning"

// Q6 — typesafe sky condition (static instances + FromProvider)
public string Value { get; }   // SkyCondition
```

## C# Standards compliance refinements

Applied after adopting `docs/engineering/csharp-standards.md`. These refine the sketch; they are not new
product questions.

| # | Refinement | Standard |
|---|------------|----------|
| DD1 | Removed `<LangVersion>latest</LangVersion>`; added `AnalysisLevel=latest` + `EnforceCodeStyleInBuild=true`; naming rules in `.editorconfig` | §2, §17 |
| DD2 | `ObservedAtUtc` is `DateTimeOffset` (an instant), not `DateTime` | §15 |
| DD3 | "City not found" is an **expected outcome** → `IWeatherProvider`/`IWeatherService` return `WeatherReport?` / `WeatherReportDto?`; the endpoint maps `null` → 404. Provider failure stays exceptional (`WeatherProviderException`) | §5 |
| DD4 | Resilience via `Microsoft.Extensions.Http.Resilience` (timeout + bounded transient retry with jitter) instead of a bare `http.Timeout` | §10 |
| DD4a | **Superseded the implementation only:** the original `AddStandardResilienceHandler()` also enabled a circuit breaker + rate limiter (deferred by `SCOPE_LATEST.yaml`) and fixed the retry count. Replaced by an explicit `AddResilienceHandler` pipeline bound from `OpenWeather:Resilience` — see e03s04 / ADR-0015. The DD4 intent (timeout + bounded retry with jitter) is unchanged. | §10, ADR-0015 |
| DD5 | Boundary guards use framework helpers (`ArgumentException.ThrowIfNullOrWhiteSpace`, `ArgumentOutOfRangeException.ThrowIfNegative`); domain invariant violations use `WeatherDomainException` | §5 |
| DD6 | `ILogger<T>` structured logging in the provider adapter; never log the URL, query string, or `appid` | §10 |
| DD7 | File-scoped namespaces, one type per file, `sealed` records/classes, primary constructors for simple DI, `CancellationToken` propagated | §3, §4, §6 |
| DD8 | Mapping failures from malformed upstream data are wrapped as `WeatherProviderException` (502), not surfaced as 500 | §10, §12 |

### Revised signatures
```csharp
public interface IWeatherProvider
{
    Task<WeatherReport?> GetCurrentWeatherAsync(string cityName, string? countryCode, CancellationToken cancellationToken);
}

public interface IWeatherService
{
    Task<WeatherReportDto?> GetCurrentWeatherAsync(string cityName, string? countryCode, CancellationToken cancellationToken);
}

public sealed record WeatherReport
{
    public DateTimeOffset ObservedAtUtc { get; }
    ...
}
```

## Test implications
- `DewPointTests`: at 100% RH, dew point ≤ temperature (exact, no tolerance).
- `TemperatureTests`: `decimal` literals; equality without tolerances.
- `OpenWeatherMapServiceTests`: payload has no `deg` → `Cardinal == "Variable"`, `DirectionDegrees is null`.
- `ApiKeyHandlerTests` / logging test: the API key never appears in exception messages or log output.
- `WeatherReportTests`: no coordinate assertion (Location is City + Country).
