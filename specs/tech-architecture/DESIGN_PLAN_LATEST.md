# Design Plan

Style: **modular monolith by bounded context** (reference: `kgrzybek/modular-monolith-with-ddd`), each module
internally Onion-layered and CQRS. See ADR-0005 (module slicing) and ADR-0006 (data isolation).

## Module context map

```
                         ┌──────────────────────────────────────────────┐
                         │ BuildingBlocks (Domain/Application/Infra)     │  shared primitives, NOT a context
                         └───────▲───────────▲───────────▲──────▲────────┘
                                 │           │           │      │
                    ┌────────────┴───┐       │           │      │
                    │ M1 Geography   │       │           │      │
                    │ owns geography │       │           │      │
                    │ .db            │       │           │      │
                    └───────┬────────┘       │           │      │
                            │ CityAddedIntegrationEvent   │      │
                            │        │                   │      │
                            │        ▼                   ▼      ▼
                            │  ┌────────────┐      ┌───────────┐
                            │  │ M3 Favorites│     │ M4 Notes  │
                            │  │ favorites.db│     │ notes.db  │
                            │  └────────────┘      └───────────┘
                            │
                    ┌───────▼────────┐
                    │ M2 Weather     │   independent — provider geocodes q={city},{country}
                    │ (core, no data)│   no module edge
                    └────────────────┘

Async edges only:  M1 → M3, M1 → M4   (via Events Bus + Outbox). No direct cross-module calls.
```

## Module APIs (used only by the API host)

```csharp
public interface IGeographyModule {
    Task<List<CountryDto>> GetCountriesAsync(CancellationToken ct);
    Task<List<CityDto>> GetCitiesAsync(string countryCode, CancellationToken ct);
}
public interface IWeatherModule {
    Task<WeatherReportDto> GetCurrentWeatherAsync(string cityName, string? countryCode, CancellationToken ct);
}
public interface IFavoritesModule {
    Task<Guid> AddFavoriteAsync(string cityName, string countryCode, CancellationToken ct);
}
public interface INotesModule {
    Task<Guid> AddNoteAsync(string cityName, string countryCode, string note, decimal? temperatureCelsius, CancellationToken ct);
}
```

## Per-module design

### M1 Geography
- Domain: `Countries.Country` (Code, Name), `Cities.City` (CityId, CountryCode, Name, GeoCoordinate). Module `SharedKernel`.
- Application: `Countries/GetCountries`, `Cities/GetCitiesByCountry`, `Contracts/IGeographyModule`, `Configuration`.
- Infrastructure: `GeographyContext` (`geography.db`), `GeographyModule`, `Outbox`, background publisher.
- Publishes `CityAddedIntegrationEvent(CityId, CountryCode, Name, Latitude, Longitude)`.
- Endpoints: `GET /api/countries`, `GET /api/countries/{countryCode}/cities`.

### M2 Weather (core)
- Domain: `WeatherReports` aggregate `WeatherReport` + VOs `Temperature`, `DewPoint`, `RelativeHumidity`, `WindInfo`, `SkyCondition`, `Visibility`, `Pressure`, `Location`.
- Application: `WeatherReports/GetCurrentWeather` (query + handler + validator), `Contracts/IWeatherModule`, `Abstractions/IWeatherService`, `Configuration`.
- Infrastructure: `OpenWeatherMapService` (typed client), `WeatherModule`. No DbContext.
- Endpoint: `GET /api/weather/{cityName}?countryCode=` → full metric payload; `404` unknown city; `502` provider down.

### M3 Favorites
- Domain: `FavoriteCities.FavoriteCity` (aggregate, unique per city), `Cities.City` (local projection).
- Application: `FavoriteCities/AddFavoriteCity`, `Cities/CityAddedIntegrationEventHandler`, `Contracts/IFavoritesModule`, `Configuration`.
- Infrastructure: `FavoritesContext` (`favorites.db`, tables `FavoriteCities`, `CityProjections`), `FavoritesModule`, `Outbox`.
- Endpoint: `POST /api/cities/favorites` → `201` + id; `409` duplicate; `400` unknown city.

### M4 Notes
- Domain: `Notes.WeatherNote` (aggregate), `Cities.City` (local projection).
- Application: `Notes/AddWeatherNote`, `Cities/CityAddedIntegrationEventHandler`, `Contracts/INotesModule`, `Configuration`.
- Infrastructure: `NotesContext` (`notes.db`, tables `WeatherNotes`, `CityProjections`), `NotesModule`, `Outbox`.
- Endpoint: `POST /api/weather/notes` → `201` + id; `400` validation / unknown city.

## Cross-module flow (write side)

```
POST /api/weather/notes
  API host ──INotesModule.AddNoteAsync──► Notes module
      Application: AddWeatherNoteCommand → validate note; resolve city from local CityProjections
      Domain: WeatherNote.Add(...)  raises WeatherNoteAddedDomainEvent
      Infrastructure: NotesContext.SaveChanges → DomainEventsDispatcher → Outbox
  ── background ── Outbox → EventsBus → (no subscribers) 
  ◄── 201 Created { id }
```

```
Startup / seed
  Geography seeds Countries+Cities → writes CityAddedIntegrationEvent to Geography Outbox
  ── background ── Outbox → EventsBus → Favorites handler + Notes handler
      each upserts its local CityProjections row
```

## API contract (planned)

| Method | Route | Module |
|--------|-------|--------|
| GET | `/api/countries` | Geography |
| GET | `/api/countries/{countryCode}/cities` | Geography |
| GET | `/api/weather/{cityName}?countryCode=` | Weather |
| POST | `/api/weather/notes` | Notes |
| POST | `/api/cities/favorites` | Favorites |

Weather `200` body:
```json
{
  "location": { "city": "London", "country": "GB" },
  "observedAtUtc": "2025-10-13T12:00:00Z",
  "temperature": { "celsius": 12.5, "fahrenheit": 54.5 },
  "dewPoint": { "celsius": 9.1, "fahrenheit": 48.4 },
  "relativeHumidityPercent": 79,
  "wind": { "speedMetersPerSecond": 4.1, "directionDegrees": 220, "cardinal": "SW" },
  "visibilityMeters": 10000,
  "pressureHectopascals": 1013,
  "skyCondition": "Cloudy"
}
```

## Error handling
ProblemDetails via `IExceptionHandler`: `BusinessRuleValidationException` → 400; `NotFoundException` → 404;
`ConflictException` → 409; `WeatherProviderException` → 502; unhandled → 500 (no stack trace in production).

## Cross-cutting
- Per-module `Configuration/CommandsExecutor` + MediatR pipeline (`ValidationBehavior`, `LoggingBehavior`).
- `IDateTimeProvider` in BuildingBlocks for deterministic timestamps.
- Each module is initialized by the host (`Add<Module>Module()` + `Initialize`).
