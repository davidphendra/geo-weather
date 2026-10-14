using System.Diagnostics.Metrics;

namespace Weather.BuildingBlocks.Application.Telemetry;

/// <summary>
/// Single source of truth for the Weather meter, its instruments, units and tag keys. Modules emit into
/// these instruments; the host subscribes with <c>AddMeter(WeatherMeters.MeterName)</c> and exports.
/// </summary>
public static class WeatherMeters
{
    public const string MeterName = "Weather";

    /// <summary>The shared meter. Modules create their own observable gauges on it.</summary>
    public static readonly Meter Meter = new(MeterName, "1.0.0");

    // MediatR (technical).
    public static readonly Counter<long> MediatRRequests =
        Meter.CreateCounter<long>("mediatr.requests", unit: "{request}");

    public static readonly Histogram<double> MediatRRequestDuration =
        Meter.CreateHistogram<double>("mediatr.request.duration", unit: "ms");

    // Integration events (technical).
    public static readonly Counter<long> IntegrationEventsPublished =
        Meter.CreateCounter<long>("integration.event.published", unit: "{event}");

    public static readonly Counter<long> IntegrationEventsHandled =
        Meter.CreateCounter<long>("integration.event.handled", unit: "{event}");

    public static readonly Counter<long> IntegrationEventsFailed =
        Meter.CreateCounter<long>("integration.event.failed", unit: "{event}");

    public static readonly Histogram<double> IntegrationEventDuration =
        Meter.CreateHistogram<double>("integration.event.duration", unit: "ms");

    // Weather (business).
    public static readonly Counter<long> WeatherReportRequests =
        Meter.CreateCounter<long>("weather.report.requests", unit: "{request}");

    public static readonly Histogram<double> WeatherReportDuration =
        Meter.CreateHistogram<double>("weather.report.duration", unit: "ms");

    public static readonly Counter<long> WeatherCityNotFound =
        Meter.CreateCounter<long>("weather.city.not_found", unit: "{request}");

    public static readonly Counter<long> WeatherProviderRequests =
        Meter.CreateCounter<long>("weather.provider.requests", unit: "{request}");

    public static readonly Counter<long> WeatherProviderFailures =
        Meter.CreateCounter<long>("weather.provider.failures", unit: "{failure}");

    public static readonly Histogram<double> WeatherProviderDuration =
        Meter.CreateHistogram<double>("weather.provider.duration", unit: "ms");

    // Geography (business).
    public static readonly Counter<long> GeographyCountryListRequests =
        Meter.CreateCounter<long>("geography.country.list_requests", unit: "{request}");

    public static readonly Counter<long> GeographyCityListRequests =
        Meter.CreateCounter<long>("geography.city.list_requests", unit: "{request}");

    // Favorites (business).
    public static readonly Counter<long> FavoritesOperations =
        Meter.CreateCounter<long>("favorites.operations", unit: "{operation}");

    public static readonly Histogram<double> FavoritesDuration =
        Meter.CreateHistogram<double>("favorites.duration", unit: "ms");

    // Notes (business).
    public static readonly Counter<long> NotesOperations =
        Meter.CreateCounter<long>("notes.operations", unit: "{operation}");

    public static readonly Histogram<double> NotesDuration =
        Meter.CreateHistogram<double>("notes.duration", unit: "ms");

    // API errors (technical + business).
    public static readonly Counter<long> ApiErrors =
        Meter.CreateCounter<long>("api.errors", unit: "{error}");

    /// <summary>Bounded attribute keys. Never add a high-cardinality value (name, id, url, message).</summary>
    public static class Tags
    {
        public const string Outcome = "weather.outcome";
        public const string CountryCode = "geo.country_code";
        public const string HasSearch = "geo.has_search";
        public const string Operation = "favorites.operation";
        public const string FavoritesOutcome = "favorites.outcome";
        public const string NotesOutcome = "notes.outcome";
        public const string RequestName = "mediatr.request.name";
        public const string RequestKind = "mediatr.request.kind";
        public const string Module = "module";
        public const string MediatROutcome = "mediatr.outcome";
        public const string EventType = "event.type";
        public const string HandlerModule = "handler.module";
        public const string ErrorType = "error.type";
        public const string ErrorCode = "error.code";
        public const string RequestMethod = "http.request.method";
        public const string Route = "http.route";
        public const string StatusCode = "http.response.status_code";
    }

    /// <summary>Closed outcome values shared by every counter.</summary>
    public static class Outcomes
    {
        public const string Success = "success";
        public const string NotFound = "not_found";
        public const string ProviderError = "provider_error";
        public const string ValidationError = "validation_error";
        public const string Error = "error";
        public const string Created = "created";
        public const string Duplicate = "duplicate";
        public const string CityNotFound = "city_not_found";
        public const string Timeout = "timeout";
        public const string Unavailable = "unavailable";
        public const string BadStatus = "bad_status";
        public const string EmptyBody = "empty_body";
    }
}
