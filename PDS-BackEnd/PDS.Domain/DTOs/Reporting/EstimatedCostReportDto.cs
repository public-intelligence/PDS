using PDS.Domain.Enums;

namespace PDS.Domain.DTOs.Reporting;

/// <summary>
/// RF-RE-03: costo estimado de la plantilla = salario (según base elegida) × plazas autorizadas.
/// Es una ESTIMACIÓN derivada de plazas autorizadas, no nómina real. El salario proviene de la
/// fotografía de la versión (StructureVersionPosition), fiel a la versión consultada.
/// </summary>
public record EstimatedCostReportDto
{
    public required ReportContextLabelDto Context { get; init; }
    public SalaryBasis Basis { get; init; }
    public ReportGrouping GroupBy { get; init; }
    public bool IsEstimate { get; init; } = true;

    public ICollection<EstimatedCostRowDto> Rows { get; init; } = [];
    public ICollection<ReportSubtotalDto> Subtotals { get; init; } = [];
    public required ReportSubtotalDto GrandTotal { get; init; }

    /// <summary>Cargos sin salario utilizable (salario en cero), excluidos del costo pero contabilizados.</summary>
    public int UnresolvedPositionCount { get; init; }

    public required string HistoricalFidelityNote { get; init; }
}

public record EstimatedCostRowDto
{
    /// <summary>Clave del grupo al que pertenece la fila (coincide con ReportSubtotalDto.GroupKey).</summary>
    public string? GroupKey { get; init; }

    public Guid UnitId { get; init; }
    public required string UnitCode { get; init; }
    public required string UnitName { get; init; }
    public int Level { get; init; }

    public Guid PositionId { get; init; }
    public required string PositionName { get; init; }
    public string? OccupationalGroupName { get; init; }
    public int Grade { get; init; }

    public int AuthorizedPosts { get; init; }

    /// <summary>Salario unitario según la base (mínimo, punto medio o máximo).</summary>
    public decimal? UnitSalary { get; init; }

    /// <summary>Costo de la línea = UnitSalary × AuthorizedPosts.</summary>
    public decimal? LineCost { get; init; }

    public bool SalaryResolved { get; init; }
}
