namespace PDS.Domain.DTOs.Planning;

/// <summary>Error de una fila/columna durante una importación de Excel (PEI, POA, matriz de códigos).</summary>
public record ImportErrorDto
{
    /// <summary>Fila del archivo (base 1, tal como la ve el usuario en Excel).</summary>
    public required int Row { get; init; }
    public string? Column { get; init; }
    public required string Message { get; init; }
}

/// <summary>Conteos del resultado de una importación.</summary>
public record ImportSummaryDto
{
    public int Added { get; init; }
    public int Updated { get; init; }
    public int Unchanged { get; init; }
    public int Removed { get; init; }
}

/// <summary>
/// Resultado del pipeline de importación en dos fases (M1 §infra Excel): con dryRun=true solo
/// valida y reporta; con dryRun=false y sin errores, aplica en una transacción.
/// </summary>
public record ImportResultDto
{
    /// <summary>True si los cambios fueron aplicados (dryRun=false y sin errores).</summary>
    public required bool Applied { get; init; }
    public required IReadOnlyCollection<ImportErrorDto> Errors { get; init; }
    public IReadOnlyCollection<string> Warnings { get; init; } = [];
    public ImportSummaryDto? Summary { get; init; }
}
