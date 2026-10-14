namespace Weather.WebUI.Configuration;

/// <summary>Where telemetry is exported. Azure Monitor is the production target (ADR-0014).</summary>
public enum ObservabilityExportTarget
{
    AzureMonitor,
    Console,
    Otlp,
}
