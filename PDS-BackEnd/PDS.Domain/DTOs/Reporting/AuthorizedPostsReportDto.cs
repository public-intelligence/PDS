using PDS.Domain.Enums;

namespace PDS.Domain.DTOs.Reporting;

/// <summary>
/// RF-RE-01: reporte de plazas autorizadas con agrupación/pivote por unidad, nivel jerárquico o
/// categoría salarial (grado). Las columnas de ocupadas/vacantes se excluyen por diseño (PDS es
/// solo definicional).
/// </summary>
public record AuthorizedPostsReportDto
{
    public required ReportContextLabelDto Context { get; init; }
    public ReportGrouping GroupBy { get; init; }
    public ICollection<ReportRowDto> Rows { get; init; } = [];
    public ICollection<ReportSubtotalDto> Subtotals { get; init; } = [];
    public required ReportSubtotalDto GrandTotal { get; init; }
}

/// <summary>Fila hoja: una asignación cargo↔unidad con su cantidad de plazas autorizadas.</summary>
public record ReportRowDto
{
    /// <summary>Clave del grupo al que pertenece la fila (coincide con ReportSubtotalDto.GroupKey).</summary>
    public string? GroupKey { get; init; }

    public Guid? UnitId { get; init; }
    public string? UnitCode { get; init; }
    public string? UnitName { get; init; }
    public string? AreaTypeName { get; init; }
    public int? Level { get; init; }

    public Guid? PositionId { get; init; }
    public string? PositionName { get; init; }
    public string? OccupationalGroupName { get; init; }

    /// <summary>Grado salarial (= categoría salarial).</summary>
    public int? Grade { get; init; }

    public int AuthorizedPosts { get; init; }
}

/// <summary>Subtotal por grupo o total general. EstimatedCost solo aplica al reporte de costo.</summary>
public record ReportSubtotalDto
{
    public required string GroupKey { get; init; }
    public required string GroupLabel { get; init; }
    public int DistinctPositionCount { get; init; }
    public int AuthorizedPosts { get; init; }
    public decimal? EstimatedCost { get; init; }
}
