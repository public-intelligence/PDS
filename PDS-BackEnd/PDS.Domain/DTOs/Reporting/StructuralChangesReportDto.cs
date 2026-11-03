using PDS.Domain.DTOs.LegalInstrument;
using PDS.Domain.DTOs.StructureVersion;
using PDS.Domain.Enums;

namespace PDS.Domain.DTOs.Reporting;

/// <summary>
/// RF-RE-05: cambios estructurales aprobados en un período, a partir de la trazabilidad del flujo de
/// aprobación (RF-FA-01). Cada versión publicada en el período = un cambio estructural aprobado,
/// enriquecido con el diff frente a su versión predecesora.
/// </summary>
public record StructuralChangesReportDto
{
    public DateOnly From { get; init; }
    public DateOnly To { get; init; }
    public required StructuralChangesFilterDto Filters { get; init; }
    public ICollection<StructuralChangeEntryDto> Entries { get; init; } = [];
}

/// <summary>Filtros del reporte (enlazados desde query string).</summary>
public record StructuralChangesFilterDto
{
    public DateOnly From { get; init; }
    public DateOnly To { get; init; }

    /// <summary>Filtra por tipo de cambio de unidad ("Created", "Deleted", "Modified", "Relocated").</summary>
    public string? ChangeType { get; init; }

    /// <summary>Filtra a versiones cuyo diff afecta esta unidad (SourceUnitId).</summary>
    public Guid? UnitId { get; init; }

    /// <summary>Filtra por revisor/aprobador/publicador.</summary>
    public Guid? ApproverId { get; init; }
}

public record StructuralChangeEntryDto
{
    public Guid VersionId { get; init; }
    public int? Number { get; init; }
    public required string Name { get; init; }
    public VersionStatus Status { get; init; }
    public DateOnly? EffectiveFrom { get; init; }

    public LegalInstrumentDto? LegalInstrument { get; init; }

    public Guid? CreatedBy { get; init; }
    public string? CreatedByName { get; init; }
    public DateTime CreatedAt { get; init; }
    public string? ReviewedByName { get; init; }
    public DateTime? ReviewedAt { get; init; }
    public string? ApprovedByName { get; init; }
    public DateTime? ApprovedAt { get; init; }
    public string? PublishedByName { get; init; }
    public DateTime? PublishedAt { get; init; }

    public ICollection<AuditLogDto> AuditLogs { get; init; } = [];

    /// <summary>Resumen del cambio de unidades frente a la versión predecesora.</summary>
    public ComparisonSummaryDto? UnitChangeSummary { get; init; }

    /// <summary>Resumen del cambio de cargos/plazas frente a la versión predecesora.</summary>
    public PositionDeltaSummaryDto? PositionDeltaSummary { get; init; }
}
