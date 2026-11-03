using PDS.Domain.DTOs.Reporting;

namespace PDS.Service.Services.Reporting;

/// <summary>
/// Modelo tabular independiente del formato. Cada reporte se proyecta a un ReportTable, de modo que
/// PDF/Excel/CSV y la pantalla derivan de la misma agregación (RF-RE-06).
/// </summary>
public sealed class ReportTable
{
    public required string Title { get; init; }
    public required ReportContextLabelDto Context { get; init; }
    public required IReadOnlyList<ReportColumn> Columns { get; init; }
    public required IReadOnlyList<ReportTableRow> Rows { get; init; }

    /// <summary>Nota al pie (p. ej. el descargo de "costo estimado").</summary>
    public string? Note { get; init; }
}

public sealed class ReportColumn
{
    public required string Header { get; init; }
    public bool IsNumeric { get; init; }
}

public sealed class ReportTableRow
{
    public required IReadOnlyList<string?> Cells { get; init; }
    public bool IsSubtotal { get; init; }
    public bool IsGrandTotal { get; init; }
    public int IndentLevel { get; init; }
}

/// <summary>Salida de un renderer: bytes + tipo de contenido + extensión.</summary>
public sealed class ReportRenderResult
{
    public required byte[] Content { get; init; }
    public required string ContentType { get; init; }
    public required string FileExtension { get; init; }
}
