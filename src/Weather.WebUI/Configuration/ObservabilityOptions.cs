using System.ComponentModel.DataAnnotations;

namespace Weather.WebUI.Configuration;

/// <summary>Bounds the OpenTelemetry wiring. Disabled means no SDK and no exporter are created.</summary>
public sealed class ObservabilityOptions
{
    public const string SectionName = "Observability";

    public bool Enabled { get; init; }

    public ObservabilityExportTarget ExportTarget { get; init; } = ObservabilityExportTarget.AzureMonitor;

    [Range(0.0, 1.0)]
    public double SamplingRatio { get; init; } = 1.0;
}
