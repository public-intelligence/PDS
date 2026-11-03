namespace PDS.Domain.Models;

/// <summary>
/// Directorio mínimo de organizaciones (tenants) conocido por PDS. PDS no es dueño del dato maestro de
/// organizaciones (vive en GobID); aquí se denormaliza — desde los claims del token al publicar — lo
/// imprescindible para resolver el organigrama público por slug sin requerir autenticación.
/// </summary>
public class OrganizationDirectory
{
    /// <summary>Identificador de la organización (tenant). Coincide con OrganizationId del resto del dominio.</summary>
    public int Id { get; set; }

    public required string Name { get; set; }

    public string? Acronym { get; set; }

    /// <summary>Identificador legible y único para URLs públicas (p. ej. "med").</summary>
    public required string Slug { get; set; }

    public DateTime UpdatedAt { get; set; }
}
