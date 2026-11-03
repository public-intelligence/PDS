using PDS.Domain.Enums;

namespace PDS.Domain.DTOs.Reporting;

/// <summary>
/// RF-RE-07: indicadores definicionales del contexto seleccionado. Reemplaza cualquier KPI de
/// ocupación por KPIs de definición (unidades, cargos, plazas autorizadas, costo estimado, calidad).
/// </summary>
public record DashboardDto
{
    public required ReportContextLabelDto Context { get; init; }

    /// <summary>Total de unidades activas, desglosado por nivel jerárquico.</summary>
    public ICollection<LevelCountDto> ActiveUnitsByLevel { get; init; } = [];
    public int TotalActiveUnits { get; init; }

    public int TotalDefinedPositions { get; init; }

    public int TotalAuthorizedPosts { get; init; }
    public ICollection<LevelCountDto> AuthorizedPostsByLevel { get; init; } = [];
    public ICollection<CategoryCountDto> AuthorizedPostsBySalaryCategory { get; init; } = [];
    public ICollection<CategoryCountDto> AuthorizedPostsByOccupationalGroup { get; init; } = [];

    public decimal? EstimatedTotalCost { get; init; }
    public bool CostIsEstimate { get; init; } = true;
    public SalaryBasis CostBasis { get; init; }

    /// <summary>Solicitudes pendientes de aprobación (versiones InReview + Approved) — RF-FA-01.</summary>
    public int PendingApprovalCount { get; init; }

    /// <summary>Cambios estructurales del último período (versión vigente vs. anterior).</summary>
    public required StructuralChangeSummaryDto RecentStructuralChanges { get; init; }

    /// <summary>Alertas de calidad definicional (reemplazo en alcance de "vacantes críticas").</summary>
    public ICollection<QualityAlertDto> QualityAlerts { get; init; } = [];
}

public record LevelCountDto
{
    public int Level { get; init; }
    public required string Label { get; init; }
    public int Count { get; init; }
    public int AuthorizedPosts { get; init; }
}

public record CategoryCountDto
{
    public required string Key { get; init; }
    public required string Label { get; init; }
    public int DistinctPositions { get; init; }
    public int AuthorizedPosts { get; init; }
}

public record StructuralChangeSummaryDto
{
    public int CreatedUnits { get; init; }
    public int ModifiedUnits { get; init; }
    public int RelocatedUnits { get; init; }
    public int DeletedUnits { get; init; }
    public Guid? FromVersionId { get; init; }
    public Guid? ToVersionId { get; init; }
}

public record QualityAlertDto
{
    /// <summary>Código estable de la alerta (p.ej. UnitNoAuthorizedPosts, UnitMissingSupervisor).</summary>
    public required string Code { get; init; }
    public required string Message { get; init; }
    public Guid? UnitId { get; init; }
    public Guid? PositionId { get; init; }
    public int Count { get; init; }
}
