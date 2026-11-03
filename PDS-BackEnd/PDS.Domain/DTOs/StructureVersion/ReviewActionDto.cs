namespace PDS.Domain.DTOs.StructureVersion;

/// <summary>
/// Cuerpo para las transiciones del flujo (enviar a revisión, aprobar, rechazar). El comentario
/// es opcional para aprobar/enviar y requerido para rechazar (validado en el servicio).
/// </summary>
public record ReviewActionDto
{
    public string? Comment { get; init; }
}
