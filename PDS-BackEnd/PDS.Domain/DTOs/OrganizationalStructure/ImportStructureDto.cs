namespace PDS.Domain.DTOs.OrganizationalStructure;

/// <summary>
/// Plantilla portable para importar una estructura organizacional completa desde un JSON.
/// Solo describe la jerarquía (nombre + tipo de área + dependencias anidadas); el backend
/// regenera los códigos e identificadores. <see cref="Version"/> y cualquier campo extra son
/// informativos y se ignoran al importar.
/// </summary>
public record ImportStructureDto
{
    public int? Version { get; init; }
    public List<ImportStructureNodeDto> Nodes { get; init; } = [];
}

/// <summary>
/// Un nodo (unidad organizativa) de la plantilla de importación.
/// </summary>
public record ImportStructureNodeDto
{
    public string Name { get; init; } = null!;

    /// <summary>Tipo de área a resolver por nombre (forma primaria, portable entre entornos).</summary>
    public string? AreaType { get; init; }

    /// <summary>Identificador explícito del tipo de área (opcional; tiene prioridad si es válido).</summary>
    public Guid? AreaTypeId { get; init; }

    public List<ImportStructureNodeDto> Children { get; init; } = [];
}
