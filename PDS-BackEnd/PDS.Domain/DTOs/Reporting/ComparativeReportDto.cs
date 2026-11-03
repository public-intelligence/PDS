using PDS.Domain.DTOs.StructureVersion;
using PDS.Domain.Enums;

namespace PDS.Domain.DTOs.Reporting;

/// <summary>
/// RF-RE-04: comparativo entre dos contextos (versiones o fechas). Reutiliza la comparación de
/// unidades (RF-VH-03, VersionComparisonDto) y agrega los deltas de cargos, plazas autorizadas y el
/// cambio en el costo estimado.
/// </summary>
public record ComparativeReportDto
{
    public required ReportContextLabelDto FromContext { get; init; }
    public required ReportContextLabelDto ToContext { get; init; }

    /// <summary>Diferencia a nivel de unidades (creadas/eliminadas/modificadas/reubicadas).</summary>
    public required VersionComparisonDto UnitComparison { get; init; }

    public required PositionDeltaSummaryDto PositionDeltas { get; init; }
    public required CostDeltaDto CostDelta { get; init; }
}

public record PositionDeltaSummaryDto
{
    public int AddedCount { get; init; }
    public int RemovedCount { get; init; }
    public int PostCountChangedCount { get; init; }
    public int FromTotalAuthorizedPosts { get; init; }
    public int ToTotalAuthorizedPosts { get; init; }
    public int AuthorizedPostsDelta { get; init; }
    public ICollection<PositionDeltaDto> Changes { get; init; } = [];
}

public record PositionDeltaDto
{
    public Guid PositionId { get; init; }
    public required string PositionName { get; init; }
    public Guid? UnitId { get; init; }

    /// <summary>"Added", "Removed" o "PostsChanged".</summary>
    public required string ChangeType { get; init; }

    public int FromAuthorizedPosts { get; init; }
    public int ToAuthorizedPosts { get; init; }
    public int AuthorizedPostsDelta { get; init; }
}

public record CostDeltaDto
{
    public SalaryBasis Basis { get; init; }
    public decimal? FromEstimatedCost { get; init; }
    public decimal? ToEstimatedCost { get; init; }
    public decimal? CostDelta { get; init; }
    public bool IsEstimate { get; init; } = true;
}
