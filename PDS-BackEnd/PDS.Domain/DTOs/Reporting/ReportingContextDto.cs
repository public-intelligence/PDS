using PDS.Domain.Enums;

namespace PDS.Domain.DTOs.Reporting;

/// <summary>
/// Contexto de reportería: la versión publicada (por Id) o la fecha de corte sobre la cual se lee la
/// estructura. Si ambos son nulos, se asume la versión vigente hoy. Se resuelve una sola vez a través
/// de la capa de versionado (IOrgChartService) y se pasa a cada reporte.
/// </summary>
public record ReportingContextDto
{
    public Guid? VersionId { get; init; }
    public DateOnly? AsOfDate { get; init; }
}

/// <summary>
/// Etiqueta del contexto resuelto: se devuelve en cada payload y se imprime en encabezados/pies y
/// nombres de archivo de las exportaciones. Garantiza que la pantalla y la exportación reflejen el
/// mismo contexto.
/// </summary>
public record ReportContextLabelDto
{
    public Guid VersionId { get; init; }
    public int? VersionNumber { get; init; }
    public required string VersionName { get; init; }
    public VersionStatus Status { get; init; }
    public DateOnly? EffectiveFrom { get; init; }
    public DateOnly? EffectiveTo { get; init; }

    /// <summary>Fecha de corte efectiva resuelta (la fecha solicitada o la vigencia de la versión).</summary>
    public DateOnly ResolvedAsOf { get; init; }

    public DateTime GeneratedAtUtc { get; init; }
    public string? GeneratedByName { get; init; }
}
