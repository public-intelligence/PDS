namespace PDS.Domain.DTOs.Reporting;

/// <summary>
/// RF-RE-02: dotación definida (plantilla). Por unidad y por nivel jerárquico: cantidad de cargos
/// distintos y total de plazas autorizadas, consolidando los descendientes. NO expone ocupación real.
/// </summary>
public record StaffingComplementReportDto
{
    public required ReportContextLabelDto Context { get; init; }
    public ICollection<StaffingComplementRowDto> Units { get; init; } = [];
    public ICollection<ReportSubtotalDto> ByLevel { get; init; } = [];
    public required ReportSubtotalDto GrandTotal { get; init; }
}

public record StaffingComplementRowDto
{
    public Guid UnitId { get; init; }
    public required string Code { get; init; }
    public required string Name { get; init; }
    public string? AreaTypeName { get; init; }
    public int Level { get; init; }
    public Guid? ParentUnitId { get; init; }

    /// <summary>Figuras propias de la unidad.</summary>
    public int OwnDistinctPositions { get; init; }
    public int OwnAuthorizedPosts { get; init; }

    /// <summary>Figuras consolidadas (unidad + descendientes).</summary>
    public int ConsolidatedDistinctPositions { get; init; }
    public int ConsolidatedAuthorizedPosts { get; init; }

    /// <summary>Integridad definicional: la unidad tiene definido su cargo supervisor (IsManager).</summary>
    public bool HasSupervisorDefined { get; init; }
}
